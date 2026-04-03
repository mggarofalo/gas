using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using GasTracker.Infrastructure.Ynab;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Api.Endpoints;

public static class YnabProxyEndpoints
{
    public static void MapYnabProxyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ynab").WithTags("YNAB").RequireAuthorization();

        group.MapGet("/plans", async (YnabTokenService tokenService, IYnabClient ynab) =>
        {
            string token;
            try { token = await tokenService.GetDecryptedTokenAsync(); }
            catch { return Results.BadRequest(new { error = "YNAB is not configured" }); }

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

        group.MapGet("/plans/{planId}/accounts", async (string planId, YnabTokenService tokenService, IYnabClient ynab) =>
        {
            string token;
            try { token = await tokenService.GetDecryptedTokenAsync(); }
            catch { return Results.BadRequest(new { error = "YNAB is not configured" }); }

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

        group.MapGet("/plans/{planId}/categories", async (string planId, YnabTokenService tokenService, IYnabClient ynab) =>
        {
            string token;
            try { token = await tokenService.GetDecryptedTokenAsync(); }
            catch { return Results.BadRequest(new { error = "YNAB is not configured" }); }

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

        group.MapGet("/accounts/cached", async (AppDbContext db, YnabTokenService tokenService, IYnabClient ynab) =>
        {
            var cached = await db.YnabAccountCache.OrderBy(a => a.Name).ToListAsync();
            if (cached.Count > 0)
                return Results.Ok(cached.Select(a => new { a.AccountId, a.Name, a.Type, a.Balance, a.FetchedAt }));

            // Cache is empty — fetch from YNAB if configured
            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.PlanId))
                return Results.Ok(Array.Empty<object>());

            string token;
            try { token = await tokenService.GetDecryptedTokenAsync(); }
            catch { return Results.Ok(Array.Empty<object>()); }

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

        group.MapPost("/accounts/refresh", async (AppDbContext db, YnabTokenService tokenService, IYnabClient ynab) =>
        {
            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.PlanId))
                return Results.BadRequest(new { error = "YNAB is not configured" });

            string token;
            try { token = await tokenService.GetDecryptedTokenAsync(); }
            catch { return Results.BadRequest(new { error = "Failed to decrypt YNAB token" }); }

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

}
