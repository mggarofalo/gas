using System.Globalization;
using System.Net;
using GasTracker.Core;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Infrastructure.Ynab;

public class YnabPushSyncService(AppDbContext db, YnabTokenService tokenService, IYnabClient ynab)
{
    public async Task PushAsync(FillUp fillUp)
    {
        var createAttempted = false;
        try
        {
            // A sync retry must never recreate a transaction already linked to this fill-up.
            // Legacy duplicate responses may have left a synced row without a transaction ID.
            if (fillUp.YnabTransactionId is not null)
            {
                fillUp.YnabSyncStatus = "synced";
                fillUp.YnabSyncError = null;
                await db.SaveChangesAsync();
                return;
            }
            if (fillUp.YnabSyncStatus == "synced")
                return;

            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is null || !settings.Enabled || string.IsNullOrWhiteSpace(settings.PlanId))
                return;
            var accountId = fillUp.YnabAccountId ?? settings.AccountId;
            if (string.IsNullOrWhiteSpace(accountId)) return;

            var token = await tokenService.GetDecryptedTokenAsync();
            var marker = YnabTransactionIdentity.MemoMarker(fillUp.Id);
            var legacyImportId = YnabTransactionIdentity.ImportId(fillUp.Id);

            // Search the whole plan: a user may have moved or edited the transaction after
            // YNAB accepted it but before GAS received/saved the response. Also recover old pushes.
            var page = await ynab.GetTransactionsAsync(token, settings.PlanId);
            var existing = page.Transactions.Where(t => t.ImportId == legacyImportId ||
                t.Memo?.Contains(marker, StringComparison.Ordinal) == true).ToList();
            if (existing.Count > 1)
                throw new InvalidOperationException("Multiple YNAB transactions have this fill-up reference. Resolve the duplicates in YNAB before retrying.");
            if (existing.Count == 1)
            {
                fillUp.YnabTransactionId = existing[0].Id;
                fillUp.YnabSyncStatus = "synced";
                fillUp.YnabSyncError = null;
                await db.SaveChangesAsync();
                return;
            }

            if (fillUp.YnabSyncStatus == "pending")
            {
                // A timeout is not evidence that YNAB rejected the write. Retries only look
                // for the existing transaction, even across process restarts.
                fillUp.YnabSyncError = "The previous send to YNAB has an unknown outcome. No matching fill-up reference was found; no new transaction was sent. Check YNAB before retrying.";
                await db.SaveChangesAsync();
                return;
            }

            var memo = FormattableString.Invariant($"{fillUp.Vehicle?.Label ?? ""}, {fillUp.OctaneRating ?? 87}, ${fillUp.PricePerGallon:F3}, {fillUp.OdometerMiles}, {marker}");
            var tx = new YnabTransaction(
                AccountId: accountId,
                Date: fillUp.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Amount: -(long)Math.Round(fillUp.TotalCost * 1000),
                PayeeName: fillUp.StationName,
                CategoryId: fillUp.YnabCategoryId ?? settings.CategoryId,
                Memo: memo);

            // Persist before the external write. The concurrency token lets only one caller
            // transition this row to pending. Omit import_id so bank imports can match it.
            fillUp.YnabSyncStatus = "pending";
            fillUp.YnabSyncError = null;
            await db.SaveChangesAsync();
            createAttempted = true;
            var result = await ynab.CreateTransactionAsync(token, settings.PlanId, tx);
            if (result.IsDuplicate || string.IsNullOrWhiteSpace(result.TransactionId))
                throw new InvalidOperationException("YNAB did not return a transaction ID. Retry to look up the fill-up reference.");

            fillUp.YnabTransactionId = result.TransactionId;
            fillUp.YnabSyncStatus = "synced";
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request won the claim or completed recovery. Never overwrite its state.
            await db.Entry(fillUp).ReloadAsync();
        }
        catch (Exception ex)
        {
            // Only an explicit rejection makes a new send safe. Transport errors, timeouts,
            // 5xx responses and failure to persist the result all leave the outcome unknown.
            var rejected = createAttempted && ex is HttpRequestException { StatusCode:
                HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or
                HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity or HttpStatusCode.TooManyRequests };
            fillUp.YnabSyncStatus = rejected ? "failed" :
                createAttempted || fillUp.YnabSyncStatus == "pending" ? "pending" : "failed";
            fillUp.YnabSyncError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { await db.Entry(fillUp).ReloadAsync(); }
            catch { /* The persisted pending state still prevents another blind send. */ }
        }
    }
}
