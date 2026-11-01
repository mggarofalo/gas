using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GasTracker.Infrastructure.Auth;

/// <summary>
/// First-run bootstrap: when the database has no users, creates the first household and its
/// Owner from the <c>AdminSeed:*</c> settings. Once any user exists this never runs again.
/// </summary>
public static class HouseholdBootstrapper
{
    public const string DefaultHouseholdName = "Default";

    public static async Task BootstrapAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<AppDbContext>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<ApplicationUser>>();

        var email = config["AdminSeed:Email"];
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogInformation("AdminSeed:Email not configured, skipping household bootstrap");
            return;
        }

        var password = config["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminSeed:Password not configured, cannot bootstrap household owner");
            return;
        }

        if (await userManager.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist, skipping household bootstrap");
            return;
        }

        // An upgraded database whose data was backfilled before any user was seeded already has
        // a member-less household; the owner joins it rather than orphaning that data.
        var household = await db.Households.OrderBy(h => h.CreatedAt).FirstOrDefaultAsync()
            ?? new Household { Name = config["AdminSeed:HouseholdName"] ?? DefaultHouseholdName };

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = config["AdminSeed:FirstName"] ?? "Admin",
            LastName = config["AdminSeed:LastName"] ?? "User",
            MustResetPassword = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Household = household,
            HouseholdRole = HouseholdRole.Owner,
        };

        // CreateAsync saves the new household and the user together.
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            if (db.Entry(household).State == EntityState.Added)
                db.Entry(household).State = EntityState.Detached;
            logger.LogError("Failed to bootstrap household owner: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation("Household {Household} bootstrapped with owner {Email}", household.Name, email);
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  Admin credentials (first launch)");
        Console.WriteLine($"  Email:    {email}");
        Console.WriteLine($"  Password: {password}");
        Console.WriteLine("  Change this password on first login.");
        Console.WriteLine("========================================");
        Console.WriteLine();
    }
}
