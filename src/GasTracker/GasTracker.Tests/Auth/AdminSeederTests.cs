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
            new IdentityErrorDescriber(), null!, null!);

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
}
