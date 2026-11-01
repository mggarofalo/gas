using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Auth;
using GasTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GasTracker.Tests.Auth;

public class HouseholdBootstrapperTests
{
    private static (IServiceProvider, UserManager<ApplicationUser>) CreateServices(Dictionary<string, string?> config)
        => CreateServices(config, TestDbContextFactory.Create());

    private static (IServiceProvider, UserManager<ApplicationUser>) CreateServices(Dictionary<string, string?> config, AppDbContext db)
    {
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
        services.AddSingleton(db);
        services.AddSingleton(userManager);
        services.AddSingleton<UserManager<ApplicationUser>>(userManager);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var provider = services.BuildServiceProvider();

        return (provider, userManager);
    }

    [Fact]
    public async Task BootstrapAsync_CreatesAdminUser_WhenNoneExists()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@test.com",
            ["AdminSeed:Password"] = "StrongPassword123!!",
        });

        await HouseholdBootstrapper.BootstrapAsync(provider);

        var user = await userManager.FindByEmailAsync("admin@test.com");
        user.Should().NotBeNull();
        user!.MustResetPassword.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task BootstrapAsync_CreatesOneHouseholdOwnedByTheBootstrapUser()
    {
        var db = TestDbContextFactory.Create();
        var (provider, userManager) = CreateServices(ValidConfig(), db);

        await HouseholdBootstrapper.BootstrapAsync(provider);

        var household = db.Households.Should().ContainSingle().Subject;
        household.Name.Should().Be(HouseholdBootstrapper.DefaultHouseholdName);
        var user = await userManager.FindByEmailAsync("admin@test.com");
        user!.HouseholdId.Should().Be(household.Id);
        user.HouseholdRole.Should().Be(HouseholdRole.Owner);
    }

    [Fact]
    public async Task BootstrapAsync_UsesConfiguredHouseholdName()
    {
        var db = TestDbContextFactory.Create();
        var config = ValidConfig();
        config["AdminSeed:HouseholdName"] = "Garofalo Garage";
        var (provider, _) = CreateServices(config, db);

        await HouseholdBootstrapper.BootstrapAsync(provider);

        db.Households.Should().ContainSingle().Which.Name.Should().Be("Garofalo Garage");
    }

    [Fact]
    public async Task BootstrapAsync_JoinsBackfilledHouseholdInsteadOfCreatingAnother()
    {
        var db = TestDbContextFactory.Create();
        var backfilled = new Household { Name = "Default" };
        db.Households.Add(backfilled);
        db.Vehicles.Add(new Vehicle { Household = backfilled, Year = 2020, Make = "Honda", Model = "Odyssey" });
        await db.SaveChangesAsync();
        var (provider, userManager) = CreateServices(ValidConfig(), db);

        await HouseholdBootstrapper.BootstrapAsync(provider);

        db.Households.Should().ContainSingle();
        var user = await userManager.FindByEmailAsync("admin@test.com");
        user!.HouseholdId.Should().Be(backfilled.Id);
        user.HouseholdRole.Should().Be(HouseholdRole.Owner);
    }

    [Fact]
    public async Task BootstrapAsync_SkipsWhenAnyUserExists_EvenWithADifferentEmail()
    {
        var db = TestDbContextFactory.Create();
        var (provider, userManager) = CreateServices(ValidConfig(), db);
        await HouseholdBootstrapper.BootstrapAsync(provider);

        var (otherProvider, _) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "someone-else@test.com",
            ["AdminSeed:Password"] = "StrongPassword123!!",
        }, db);
        await HouseholdBootstrapper.BootstrapAsync(otherProvider);

        userManager.Users.Should().ContainSingle().Which.Email.Should().Be("admin@test.com");
        db.Households.Should().ContainSingle();
    }

    private static Dictionary<string, string?> ValidConfig() => new()
    {
        ["AdminSeed:Email"] = "admin@test.com",
        ["AdminSeed:Password"] = "StrongPassword123!!",
    };

    [Fact]
    public async Task BootstrapAsync_SkipsIfUserAlreadyExists()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@test.com",
            ["AdminSeed:Password"] = "StrongPassword123!!",
        });

        // Seed once
        await HouseholdBootstrapper.BootstrapAsync(provider);
        var user = await userManager.FindByEmailAsync("admin@test.com");
        var createdAt = user!.CreatedAt;

        // Seed again — should skip
        await HouseholdBootstrapper.BootstrapAsync(provider);
        var same = await userManager.FindByEmailAsync("admin@test.com");
        same!.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public async Task BootstrapAsync_SkipsIfEmailNotConfigured()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>());

        await HouseholdBootstrapper.BootstrapAsync(provider);

        var users = userManager.Users.ToList();
        users.Should().BeEmpty();
        provider.GetRequiredService<AppDbContext>().Households.Should().BeEmpty();
    }

    [Fact]
    public async Task BootstrapAsync_SkipsIfPasswordNotConfigured()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@test.com",
        });

        await HouseholdBootstrapper.BootstrapAsync(provider);

        var user = await userManager.FindByEmailAsync("admin@test.com");
        user.Should().BeNull();
    }

    [Fact]
    public async Task BootstrapAsync_DoesNotModifyExistingUser()
    {
        var (provider, userManager) = CreateServices(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "admin@test.com",
            ["AdminSeed:Password"] = "StrongPassword123!!",
        });

        await HouseholdBootstrapper.BootstrapAsync(provider);
        var user = await userManager.FindByEmailAsync("admin@test.com");

        // Simulate user changing their password
        user.Should().NotBeNull();
        await userManager.RemovePasswordAsync(user!);
        await userManager.AddPasswordAsync(user!, "UserChosenPassword99!!");
        user!.MustResetPassword = false;
        await userManager.UpdateAsync(user);

        // Seed again — should not touch the user at all
        await HouseholdBootstrapper.BootstrapAsync(provider);
        var same = await userManager.FindByEmailAsync("admin@test.com");
        same!.MustResetPassword.Should().BeFalse();
        (await userManager.CheckPasswordAsync(same, "UserChosenPassword99!!")).Should().BeTrue();
    }
}
