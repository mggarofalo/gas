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
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
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
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
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
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/accounts/cached", async (AppDbContext db, IDataProtectionProvider dp, IYnabClient ynab) =>
        {
            var cached = await db.YnabAccountCache.OrderBy(a => a.Name).ToListAsync();
            if (cached.Count > 0)
                return Results.Ok(cached.Select(a => new { a.AccountId, a.Name, a.Type, a.Balance, a.FetchedAt }));

            // Cache is empty — fetch from YNAB if configured
            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.PlanId))
                return Results.Ok(Array.Empty<object>());

            var token = await GetDecryptedToken(db, dp);
            if (token is null)
                return Results.Ok(Array.Empty<object>());

            try
            {
                var accounts = await ynab.GetAccountsAsync(token, settings.PlanId);
                return Results.Ok(await RefreshCache(db, accounts));
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/accounts/refresh", async (AppDbContext db, IDataProtectionProvider dp, IYnabClient ynab) =>
        {
            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.PlanId))
                return Results.BadRequest(new { error = "YNAB is not configured" });

            var token = await GetDecryptedToken(db, dp);
            if (token is null)
                return Results.BadRequest(new { error = "Failed to decrypt YNAB token" });

            try
            {
                var accounts = await ynab.GetAccountsAsync(token, settings.PlanId);
                return Results.Ok(await RefreshCache(db, accounts));
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    private static async Task<object[]> RefreshCache(AppDbContext db, List<YnabAccount> accounts)
    {
        var now = DateTimeOffset.UtcNow;
        db.YnabAccountCache.RemoveRange(await db.YnabAccountCache.ToListAsync());
        var cached = accounts.Select(a => new Core.Entities.YnabAccountCache
        {
            AccountId = a.Id,
            Name = a.Name,
            Type = a.Type,
            Balance = a.Balance,
            FetchedAt = now,
        }).ToList();
        db.YnabAccountCache.AddRange(cached);
        await db.SaveChangesAsync();
        return cached.Select(a => new { a.AccountId, a.Name, a.Type, a.Balance, a.FetchedAt } as object).ToArray();
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
