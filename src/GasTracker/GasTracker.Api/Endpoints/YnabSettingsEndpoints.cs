using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Api.Endpoints;

public static class YnabSettingsEndpoints
{
    private const string Purpose = "YnabApiToken";
    // Fixed singleton ID prevents duplicate inserts from concurrent requests
    private static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static void MapYnabSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings/ynab").WithTags("YnabSettings").RequireAuthorization();

        group.MapGet("/", async (AppDbContext db, IDataProtectionProvider dp) =>
        {
            var settings = await db.YnabSettings.FirstOrDefaultAsync();
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
                maskedToken = "(decrypt failed)";
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

        group.MapPut("/", async (YnabSettingsRequest req, AppDbContext db, IDataProtectionProvider dp) =>
        {
            if (string.IsNullOrWhiteSpace(req.ApiToken))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["apiToken"] = ["API token is required"] });

            var protector = dp.CreateProtector(Purpose);
            var encryptedToken = protector.Protect(req.ApiToken);

            var settings = await db.YnabSettings.FindAsync(SingletonId);
            if (settings is null)
            {
                settings = new YnabSettings { Id = SingletonId, ApiToken = encryptedToken };
                db.YnabSettings.Add(settings);
            }
            else
            {
                settings.ApiToken = encryptedToken;
            }

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

        group.MapDelete("/", async (AppDbContext db) =>
        {
            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is not null)
            {
                db.YnabSettings.Remove(settings);
                await db.SaveChangesAsync();
            }
            return Results.NoContent();
        });
    }

    private record YnabSettingsRequest(
        string ApiToken,
        string? PlanId,
        string? PlanName,
        string? AccountId,
        string? AccountName,
        string? CategoryId,
        string? CategoryName,
        bool Enabled);
}
