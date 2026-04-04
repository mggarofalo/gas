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

        group.MapGet("/categories/cached", async (AppDbContext db, YnabTokenService tokenService, IYnabClient ynab) =>
        {
            var cached = await db.YnabCategoryCache.OrderBy(c => c.CategoryGroupName).ThenBy(c => c.Name).ToListAsync();
            if (cached.Count > 0)
                return Results.Ok(cached.Select(c => new { c.CategoryId, c.Name, c.CategoryGroupName, c.FetchedAt }));

            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.PlanId))
                return Results.Ok(Array.Empty<object>());

            string token;
            try { token = await tokenService.GetDecryptedTokenAsync(); }
            catch { return Results.Ok(Array.Empty<object>()); }

            try
            {
                var categories = await ynab.GetCategoriesAsync(token, settings.PlanId);
                return Results.Ok(await RefreshCategoryCache(db, categories));
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/categories/refresh", async (AppDbContext db, YnabTokenService tokenService, IYnabClient ynab) =>
        {
            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.PlanId))
                return Results.BadRequest(new { error = "YNAB is not configured" });

            string token;
            try { token = await tokenService.GetDecryptedTokenAsync(); }
            catch { return Results.BadRequest(new { error = "Failed to decrypt YNAB token" }); }

            try
            {
                var categories = await ynab.GetCategoriesAsync(token, settings.PlanId);
                return Results.Ok(await RefreshCategoryCache(db, categories));
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    private static async Task<object[]> RefreshCategoryCache(AppDbContext db, List<YnabCategory> categories)
    {
        var now = DateTimeOffset.UtcNow;
        db.YnabCategoryCache.RemoveRange(await db.YnabCategoryCache.ToListAsync());
        var cached = categories.Select(c => new Core.Entities.YnabCategoryCache
        {
            CategoryId = c.Id,
            Name = c.Name,
            CategoryGroupName = c.CategoryGroupName,
            FetchedAt = now,
        }).ToList();
        db.YnabCategoryCache.AddRange(cached);
        await db.SaveChangesAsync();
        return cached.Select(c => new { c.CategoryId, c.Name, c.CategoryGroupName, c.FetchedAt } as object).ToArray();
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
