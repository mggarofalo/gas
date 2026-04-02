using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GasTracker.Infrastructure.Paperless;

public class PaperlessSyncService(
    IServiceScopeFactory scopeFactory,
    IOptions<PaperlessOptions> options,
    ILogger<PaperlessSyncService> logger) : BackgroundService
{
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);
    private const int MaxAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled || string.IsNullOrWhiteSpace(options.Value.Token))
        {
            logger.LogInformation("Paperless sync disabled (enabled={Enabled}, token={HasToken})",
                options.Value.Enabled, !string.IsNullOrWhiteSpace(options.Value.Token));
            return;
        }

        logger.LogInformation("Paperless sync service started (poll every {Interval}s)", _pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Paperless sync tick failed");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var paperless = scope.ServiceProvider.GetRequiredService<IPaperlessClient>();
        var receiptStore = scope.ServiceProvider.GetRequiredService<IReceiptStore>();

        var now = DateTimeOffset.UtcNow;
        var pending = await db.FillUps
            .Include(f => f.Vehicle)
            .Where(f => f.PaperlessSyncStatus == "pending" && f.PaperlessSyncAttempts < MaxAttempts)
            .Where(f => f.ReceiptPath != null)
            .ToListAsync(ct);

        foreach (var fillUp in pending)
        {
            // Exponential backoff: skip if not enough time has passed
            var backoffSeconds = Math.Pow(2, fillUp.PaperlessSyncAttempts) * 30;
            if (fillUp.UpdatedAt.AddSeconds(backoffSeconds) > now)
                continue;

            try
            {
                await using var stream = await receiptStore.DownloadAsync(fillUp.ReceiptPath!);

                var title = $"Gas Receipt — {fillUp.StationName} — {fillUp.Date:yyyy-MM-dd}";
                var tags = new List<string> { "gas-receipt" };
                if (fillUp.Vehicle is not null)
                    tags.Add(fillUp.Vehicle.Label);
                tags.Add(fillUp.StationName);

                var fileName = Path.GetFileName(fillUp.ReceiptPath!);
                var docId = await paperless.UploadDocumentAsync(stream, fileName, title, fillUp.Date, tags);

                fillUp.PaperlessSyncStatus = "synced";
                fillUp.PaperlessDocumentId = docId > 0 ? docId : null;
                fillUp.PaperlessSyncedAt = DateTimeOffset.UtcNow;
                fillUp.PaperlessSyncError = null;

                logger.LogInformation("Synced fill-up {FillUpId} to Paperless (doc {DocId})", fillUp.Id, docId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                fillUp.PaperlessSyncAttempts++;
                fillUp.PaperlessSyncError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;

                if (fillUp.PaperlessSyncAttempts >= MaxAttempts)
                {
                    fillUp.PaperlessSyncStatus = "failed";
                    logger.LogWarning("Paperless sync failed permanently for fill-up {FillUpId}: {Error}", fillUp.Id, ex.Message);
                }
                else
                {
                    logger.LogWarning("Paperless sync attempt {Attempt}/{Max} failed for fill-up {FillUpId}: {Error}",
                        fillUp.PaperlessSyncAttempts, MaxAttempts, fillUp.Id, ex.Message);
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
