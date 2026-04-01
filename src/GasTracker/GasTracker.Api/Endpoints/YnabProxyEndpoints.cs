using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Api.Endpoints;

public static class YnabProxyEndpoints
{
    private const string TokenPurpose = "YnabApiToken";

    public static void MapYnabProxyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ynab").WithTags("YNAB").RequireAuthorization();

        group.MapGet("/plans", async (AppDbContext db, IDataProtectionProvider dp, IYnabClient ynab) =>
        {
            var token = await GetDecryptedToken(db, dp);
            if (token is null)
                return Results.BadRequest(new { error = "YNAB is not configured" });

            try
            {
                var plans = await ynab.GetPlansAsync(token);
                return Results.Ok(plans);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/plans/{planId}/accounts", async (string planId, AppDbContext db, IDataProtectionProvider dp, IYnabClient ynab) =>
        {
            var token = await GetDecryptedToken(db, dp);
            if (token is null)
                return Results.BadRequest(new { error = "YNAB is not configured" });

            try
            {
                var accounts = await ynab.GetAccountsAsync(token, planId);
                return Results.Ok(accounts);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/plans/{planId}/categories", async (string planId, AppDbContext db, IDataProtectionProvider dp, IYnabClient ynab) =>
        {
            var token = await GetDecryptedToken(db, dp);
            if (token is null)
                return Results.BadRequest(new { error = "YNAB is not configured" });

            try
            {
                var categories = await ynab.GetCategoriesAsync(token, planId);
                return Results.Ok(categories);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    private static async Task<string?> GetDecryptedToken(AppDbContext db, IDataProtectionProvider dp)
    {
        var settings = await db.YnabSettings.FirstOrDefaultAsync();
        if (settings is null) return null;

        try
        {
            var protector = dp.CreateProtector(TokenPurpose);
            return protector.Unprotect(settings.ApiToken);
        }
        catch
        {
            return null;
        }
    }
}
