using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GasTracker.Tests.Auth;

public class AdminSeederTests
{
    private static (IServiceProvider, UserManager<ApplicationUser>) CreateServices(Dictionary<string, string?> config)
    {
        var db = TestDbContextFactory.Create();
        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(db);
        var userManager = new UserManager<ApplicationUser>(
            store, null!, new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(userManager);
        services.AddSingleton<UserManager<ApplicationUser>>(userManager);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var provider = services.BuildServiceProvider();

        return (provider, userManager);
    }

    [Fact]
    public async Task SeedAsync_CreatesAdminUser_WhenNoneExists()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@test.com",
            ["AdminSeed:Password"] = "StrongPassword123!!",
        });

        await AdminSeeder.SeedAsync(provider);

        var user = await userManager.FindByEmailAsync("admin@test.com");
        user.Should().NotBeNull();
        user!.MustResetPassword.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_SkipsIfUserAlreadyExists()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@test.com",
            ["AdminSeed:Password"] = "StrongPassword123!!",
        });

        // Seed once
        await AdminSeeder.SeedAsync(provider);
        var user = await userManager.FindByEmailAsync("admin@test.com");
        var createdAt = user!.CreatedAt;

        // Seed again — should skip
        await AdminSeeder.SeedAsync(provider);
        var same = await userManager.FindByEmailAsync("admin@test.com");
        same!.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public async Task SeedAsync_SkipsIfEmailNotConfigured()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>());

        await AdminSeeder.SeedAsync(provider);

        var users = userManager.Users.ToList();
        users.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_SkipsIfPasswordNotConfigured()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@test.com",
        });

        await AdminSeeder.SeedAsync(provider);

        var user = await userManager.FindByEmailAsync("admin@test.com");
        user.Should().BeNull();
    }

    [Fact]
    public async Task SeedAsync_ResetsPasswordIfSecretChanged()
    {
        // Seed with original password
        var (provider1, userManager1) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@test.com",
            ["AdminSeed:Password"] = "OriginalPassword123!!",
        });
        await AdminSeeder.SeedAsync(provider1);

        var user = await userManager1.FindByEmailAsync("admin@test.com");
        user.Should().NotBeNull();
        (await userManager1.CheckPasswordAsync(user!, "OriginalPassword123!!")).Should().BeTrue();

        // Re-seed with different password using same DB (same userManager)
        var config2 = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminSeed:Email"] = "admin@test.com",
                ["AdminSeed:Password"] = "NewPassword456!!@@",
            })
            .Build();

        var services2 = new ServiceCollection();
        services2.AddSingleton<IConfiguration>(config2);
        services2.AddSingleton(userManager1);
        services2.AddSingleton<UserManager<ApplicationUser>>(userManager1);
        services2.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services2.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var provider2 = services2.BuildServiceProvider();

        await AdminSeeder.SeedAsync(provider2);

        // Old password should no longer work, new one should
        (await userManager1.CheckPasswordAsync(user!, "OriginalPassword123!!")).Should().BeFalse();
        (await userManager1.CheckPasswordAsync(user!, "NewPassword456!!@@")).Should().BeTrue();
        user!.MustResetPassword.Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_DoesNotResetPasswordIfAlreadyCorrect()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@test.com",
            ["AdminSeed:Password"] = "StrongPassword123!!",
        });

        await AdminSeeder.SeedAsync(provider);
        var user = await userManager.FindByEmailAsync("admin@test.com");
        user!.MustResetPassword = false;
        await userManager.UpdateAsync(user);

        // Seed again with same password — should not reset MustResetPassword
        await AdminSeeder.SeedAsync(provider);
        var same = await userManager.FindByEmailAsync("admin@test.com");
        same!.MustResetPassword.Should().BeFalse();
    }
}
