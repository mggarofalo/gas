using GasTracker.Core;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Infrastructure.Ynab;

public record PullSyncResult(int NewImports, int Skipped, int Errors, List<string> ErrorMessages);

public class YnabPullSyncService(AppDbContext db, IYnabClient ynab)
{
    /// <summary>
    /// Pull transactions from YNAB into the import review queue.
    /// Token must be decrypted by the caller.
    /// </summary>
    public async Task<PullSyncResult> PullAsync(string token, string planId, string accountId, DateOnly? sinceDate = null, long? lastServerKnowledge = null)
    {
        var page = await ynab.GetTransactionsAsync(token, planId, accountId, sinceDate, lastServerKnowledge);

        var newImports = 0;
        var skipped = 0;
        var errors = 0;
        var errorMessages = new List<string>();

        // Bulk-load existing IDs for dedup
        var existingYnabTxIds = await db.YnabImports
            .Select(i => i.YnabTransactionId)
            .ToHashSetAsync();
        var existingFillUpYnabIds = await db.FillUps
            .Where(f => f.YnabTransactionId != null)
            .Select(f => f.YnabTransactionId!)
            .ToHashSetAsync();

        foreach (var tx in page.Transactions)
        {
            // Skip our own pushes
            if (tx.ImportId is not null && tx.ImportId.StartsWith("GAS:", StringComparison.Ordinal))
            {
                skipped++;
                continue;
            }

            // Skip already imported
            if (existingYnabTxIds.Contains(tx.Id) || existingFillUpYnabIds.Contains(tx.Id))
            {
                skipped++;
                continue;
            }

            // Only import outflows (negative amounts)
            if (tx.Amount >= 0)
            {
                skipped++;
                continue;
            }

            try
            {
                var parsed = MemoParser.Parse(tx.Memo);
                var totalCost = Math.Abs(tx.Amount) / 1000m;

                var import = new YnabImport
                {
                    YnabTransactionId = tx.Id,
                    Date = DateOnly.Parse(tx.Date),
                    PayeeName = tx.PayeeName ?? "Unknown",
                    AmountMilliunits = tx.Amount,
                    Memo = tx.Memo,
                    Status = "pending",
                };

                if (parsed is not null)
                {
                    import.VehicleName = parsed.VehicleName;
                    import.OctaneRating = parsed.OctaneRating;
                    import.PricePerGallon = parsed.PricePerGallon;
                    import.OdometerMiles = parsed.OdometerMiles;
                    import.Gallons = parsed.Gallons ?? (parsed.PricePerGallon > 0
                        ? Math.Round(totalCost / parsed.PricePerGallon.Value, 3)
                        : null);
                }

                db.YnabImports.Add(import);
                existingYnabTxIds.Add(tx.Id);
                newImports++;
            }
            catch (Exception ex)
            {
                errors++;
                errorMessages.Add($"Transaction {tx.Date} {tx.PayeeName}: {ex.Message}");
            }
        }

        // Update server knowledge for delta sync
        var settings = await db.YnabSettings.FirstOrDefaultAsync();
        if (settings is not null)
            settings.LastServerKnowledge = page.ServerKnowledge;

        if (newImports > 0 || settings is not null)
            await db.SaveChangesAsync();

        return new PullSyncResult(newImports, skipped, errors, errorMessages);
    }
}
