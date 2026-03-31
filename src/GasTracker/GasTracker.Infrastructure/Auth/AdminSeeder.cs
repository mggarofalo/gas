using GasTracker.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GasTracker.Infrastructure.Auth;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<ApplicationUser>>();

        var email = config["AdminSeed:Email"];
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogInformation("AdminSeed:Email not configured, skipping admin seed");
            return;
        }

        var password = config["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminSeed:Password not configured, cannot seed admin user");
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Ensure password matches the current secret (handles secret regeneration)
            if (!await userManager.CheckPasswordAsync(existing, password))
            {
                var removeResult = await userManager.RemovePasswordAsync(existing);
                if (!removeResult.Succeeded)
                {
                    logger.LogError("Failed to remove admin password: {Errors}",
                        string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return;
                }
                var addResult = await userManager.AddPasswordAsync(existing, password);
                if (addResult.Succeeded)
                {
                    existing.MustResetPassword = true;
                    await userManager.UpdateAsync(existing);
                    logger.LogInformation("Admin password reset to match current secret for {Email}", email);
                }
                else
                {
                    logger.LogError("Failed to set admin password: {Errors}",
                        string.Join(", ", addResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Admin user {Email} already exists with correct password", email);
            }
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = config["AdminSeed:FirstName"] ?? "Admin",
            LastName = config["AdminSeed:LastName"] ?? "User",
            MustResetPassword = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation("Admin user seeded: {Email}", email);
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
