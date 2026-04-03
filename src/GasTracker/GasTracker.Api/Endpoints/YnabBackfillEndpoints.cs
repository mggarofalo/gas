using GasTracker.Core;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Api.Endpoints;

public static class YnabBackfillEndpoints
{
    public static void MapYnabBackfillEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/ynab-backfill", async (BackfillRequest req, AppDbContext db, IDataProtectionProvider dp, IYnabClient ynab, IVehicleRepository vehicleRepo) =>
        {
            // Require YNAB configured
            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.PlanId) || string.IsNullOrWhiteSpace(settings.AccountId))
                return Results.BadRequest(new { error = "YNAB is not configured with a plan and account" });

            // Decrypt token
            string token;
            try
            {
                var protector = dp.CreateProtector("YnabApiToken");
                token = protector.Unprotect(settings.ApiToken);
            }
            catch
            {
                return Results.BadRequest(new { error = "Failed to decrypt YNAB API token" });
            }

            // Validate vehicle mappings
            if (req.VehicleMappings is null || req.VehicleMappings.Count == 0)
                return Results.BadRequest(new { error = "vehicleMappings is required (vehicle name → UUID)" });

            foreach (var (name, id) in req.VehicleMappings)
            {
                var vehicle = await vehicleRepo.GetByIdAsync(id);
                if (vehicle is null)
                    return Results.BadRequest(new { error = $"Vehicle UUID '{id}' for '{name}' not found" });
            }

            // Fetch transactions from YNAB
            List<YnabTransactionRead> transactions;
            try
            {
                var sinceDate = req.SinceDate is not null ? DateOnly.Parse(req.SinceDate) : (DateOnly?)null;
                transactions = await ynab.GetTransactionsAsync(token, settings.PlanId, settings.AccountId, sinceDate);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
            {
                return Results.BadRequest(new { error = $"Failed to fetch YNAB transactions: {ex.Message}" });
            }

            var total = transactions.Count;
            var matched = 0;
            var imported = 0;
            var skipped = 0;
            var failed = 0;
            var errors = new List<string>();
            var warnings = new List<string>();
            var preview = new List<object>();

            foreach (var tx in transactions)
            {
                if (string.IsNullOrWhiteSpace(tx.Memo))
                    continue;

                var parsed = MemoParser.Parse(tx.Memo);
                if (parsed is null)
                    continue;

                matched++;
                var (vehicleName, octane, price, mileage) = parsed.Value;

                if (!req.VehicleMappings.TryGetValue(vehicleName, out var vehicleId))
                {
                    errors.Add($"Unmapped vehicle '{vehicleName}' in transaction {tx.Id}");
                    failed++;
                    continue;
                }

                var date = DateOnly.Parse(tx.Date);
                var totalCost = Math.Abs(tx.Amount) / 1000m;
                var gallons = price > 0 ? Math.Round(totalCost / price, 3) : 0m;

                // Warnings
                if (gallons > 50) warnings.Add($"Transaction {tx.Date} {tx.PayeeName}: gallons={gallons} (>50)");
                if (price < 1.00m) warnings.Add($"Transaction {tx.Date} {tx.PayeeName}: price=${price} (<$1.00)");
                if (price > 10.00m) warnings.Add($"Transaction {tx.Date} {tx.PayeeName}: price=${price} (>$10.00)");

                // Dedup
                var exists = await db.FillUps.AnyAsync(f =>
                    f.VehicleId == vehicleId && f.Date == date && f.OdometerMiles == mileage);
                if (exists)
                {
                    skipped++;
                    continue;
                }

                if (req.DryRun)
                {
                    preview.Add(new
                    {
                        date = date.ToString("yyyy-MM-dd"),
                        station = tx.PayeeName ?? "",
                        vehicleName,
                        vehicleId,
                        gallons,
                        pricePerGallon = price,
                        totalCost,
                        octane,
                        mileage,
                    });
                }
                else
                {
                    db.FillUps.Add(new FillUp
                    {
                        VehicleId = vehicleId,
                        Date = date,
                        OdometerMiles = mileage,
                        Gallons = gallons,
                        PricePerGallon = price,
                        TotalCost = totalCost,
                        OctaneRating = octane,
                        StationName = tx.PayeeName ?? "Unknown",
                    });
                }

                imported++;
            }

            if (!req.DryRun && imported > 0)
                await db.SaveChangesAsync();

            return Results.Ok(new
            {
                total,
                matched,
                imported,
                skipped,
                failed,
                dryRun = req.DryRun,
                errors,
                warnings,
                preview = req.DryRun ? preview : null,
            });
        }).RequireAuthorization();
    }

    private record BackfillRequest(
        string? SinceDate,
        Dictionary<string, Guid> VehicleMappings,
        bool DryRun = true);
}
