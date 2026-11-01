using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Api.Endpoints;

public static class YnabSettingsEndpoints
{
    private const string Purpose = "YnabApiToken";

    public static void MapYnabSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings/ynab").WithTags("YnabSettings").RequireAuthorization();

        // Each household has its own connection; the unique household_id index rejects
        // a duplicate insert from concurrent first-time token saves.
        group.MapGet("/", async (AppDbContext db, IDataProtectionProvider dp, ICurrentHousehold household) =>
        {
            if (household.HouseholdId is not { } householdId) return Results.Unauthorized();

            var settings = await db.YnabSettings.FirstOrDefaultAsync(s => s.HouseholdId == householdId);
            if (settings is null)
                return Results.Ok(new { configured = false });

            var protector = dp.CreateProtector(Purpose);
            string? maskedToken = null;
            try
            {
                var token = protector.Unprotect(settings.ApiToken);
                maskedToken = token.Length > 4 ? $"****{token[^4..]}" : "****";
            }
            catch
            {
                maskedToken = "(decrypt failed — re-enter token)";
            }

            return Results.Ok(new
            {
                configured = true,
                maskedToken,
                settings.PlanId,
                settings.PlanName,
                settings.AccountId,
                settings.AccountName,
                settings.CategoryId,
                settings.CategoryName,
                settings.Enabled,
            });
        });

        // Set/replace the YNAB API token (separate from settings to avoid accidental overwrite)
        group.MapPut("/token", async (TokenRequest req, AppDbContext db, IDataProtectionProvider dp, ICurrentHousehold household) =>
        {
            if (string.IsNullOrWhiteSpace(req.ApiToken))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["apiToken"] = ["API token is required"] });

            if (household.HouseholdId is not { } householdId) return Results.Unauthorized();

            var protector = dp.CreateProtector(Purpose);
            var settings = await db.YnabSettings.FirstOrDefaultAsync(s => s.HouseholdId == householdId);
            if (settings is null)
            {
                settings = new YnabSettings { HouseholdId = householdId, ApiToken = protector.Protect(req.ApiToken) };
                db.YnabSettings.Add(settings);
            }
            else
            {
                settings.ApiToken = protector.Protect(req.ApiToken);
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { configured = true });
        });

        // Update settings (plan, account, category, enabled) — never touches the token
        group.MapPut("/", async (YnabSettingsRequest req, AppDbContext db, ICurrentHousehold household) =>
        {
            if (household.HouseholdId is not { } householdId) return Results.Unauthorized();

            var settings = await db.YnabSettings.FirstOrDefaultAsync(s => s.HouseholdId == householdId);
            if (settings is null)
                return Results.BadRequest(new { error = "Set a token first via PUT /api/settings/ynab/token" });

            settings.PlanId = req.PlanId;
            settings.PlanName = req.PlanName;
            settings.AccountId = req.AccountId;
            settings.AccountName = req.AccountName;
            settings.CategoryId = req.CategoryId;
            settings.CategoryName = req.CategoryName;
            settings.Enabled = req.Enabled;

            await db.SaveChangesAsync();
            return Results.Ok(new { configured = true, settings.Enabled });
        });

        group.MapDelete("/", async (AppDbContext db, ICurrentHousehold household) =>
        {
            if (household.HouseholdId is not { } householdId) return Results.Unauthorized();

            var settings = await db.YnabSettings.FirstOrDefaultAsync(s => s.HouseholdId == householdId);
            if (settings is not null)
            {
                db.YnabSettings.Remove(settings);
                await db.SaveChangesAsync();
            }
            return Results.NoContent();
        });
    }

    private record TokenRequest(string ApiToken);

    private record YnabSettingsRequest(
        string? PlanId,
        string? PlanName,
        string? AccountId,
        string? AccountName,
        string? CategoryId,
        string? CategoryName,
        bool Enabled);
}
