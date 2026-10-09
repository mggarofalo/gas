using System.Net;
using FluentAssertions;
using GasTracker.Core;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using GasTracker.Infrastructure.Ynab;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;

namespace GasTracker.Tests.Ynab;

public class YnabPushSyncServiceTests : IDisposable
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly EphemeralDataProtectionProvider _protection = new();
    private readonly Mock<IYnabClient> _ynab = new(MockBehavior.Strict);
    private readonly List<YnabTransactionRead> _transactions = [];

    private YnabPushSyncService Service(AppDbContext? db = null) =>
        new(db ?? _db, new YnabTokenService(db ?? _db, _protection), _ynab.Object);

    private async Task<FillUp> SeedAsync(string status = "none", string? transactionId = null)
    {
        _db.YnabSettings.Add(new YnabSettings
        {
            ApiToken = _protection.CreateProtector("YnabApiToken").Protect("token"),
            PlanId = "plan", AccountId = "default-account", CategoryId = "default-category", Enabled = true,
        });
        var vehicle = new Vehicle { Year = 2022, Make = "Toyota", Model = "Sienna" };
        var fillUp = new FillUp
        {
            Vehicle = vehicle, Date = new DateOnly(2026, 10, 9), TotalCost = 36.74m,
            PricePerGallon = 3.499m, Gallons = 10.5m, OdometerMiles = 12345,
            StationName = "Gas station", YnabAccountId = "account", YnabCategoryId = "category",
            YnabSyncStatus = status, YnabTransactionId = transactionId,
        };
        _db.FillUps.Add(fillUp);
        await _db.SaveChangesAsync();
        _ynab.Setup(y => y.GetTransactionsAsync("token", "plan", null, null, null))
            .ReturnsAsync(() => new YnabTransactionPage(_transactions.ToList(), 1));
        return fillUp;
    }

    private YnabTransactionRead Remote(FillUp fillUp, bool legacy = false) =>
        new("remote-id", "2026-10-10", -36740, "Renamed payee",
            legacy ? "Old memo" : $"Edited memo, {YnabTransactionIdentity.MemoMarker(fillUp.Id)}",
            "category", "moved-account", legacy ? YnabTransactionIdentity.ImportId(fillUp.Id) : null);

    [Fact]
    public async Task NewFillUp_IsUserEnteredUnclearedAndSavedBeforeSending()
    {
        var fillUp = await SeedAsync();
        YnabTransaction? sent = null;
        _ynab.Setup(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()))
            .Returns(async (string _, string _, YnabTransaction tx) =>
            {
                sent = tx;
                (await _db.FillUps.AsNoTracking().SingleAsync()).YnabSyncStatus.Should().Be("pending");
                return new YnabTransactionResult("remote-id", false);
            });

        await Service().PushAsync(fillUp);
        // A second call must not POST again.
        await Service().PushAsync(fillUp);

        sent.Should().NotBeNull();
        sent!.ImportId.Should().BeNull();
        sent.Cleared.Should().Be("uncleared");
        sent.Approved.Should().BeTrue();
        sent.AccountId.Should().Be("account");
        sent.CategoryId.Should().Be("category");
        sent.Amount.Should().Be(-36740);
        sent.Date.Should().Be("2026-10-09");
        sent.Memo.Should().Contain(YnabTransactionIdentity.MemoMarker(fillUp.Id));
        MemoParser.Parse(sent.Memo)!.OdometerMiles.Should().Be(12345);
        fillUp.YnabTransactionId.Should().Be("remote-id");
        fillUp.YnabSyncStatus.Should().Be("synced");
        _ynab.Verify(y => y.CreateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<YnabTransaction>()), Times.Once);
    }

    [Theory]
    [InlineData("pending", false)]
    [InlineData("failed", true)]
    public async Task Retry_RecoversMemoMarkerOrLegacyImportId(string status, bool legacy)
    {
        var fillUp = await SeedAsync(status);
        _transactions.Add(Remote(fillUp, legacy));

        await Service().PushAsync(fillUp);

        fillUp.YnabTransactionId.Should().Be("remote-id");
        fillUp.YnabSyncStatus.Should().Be("synced");
        fillUp.YnabSyncError.Should().BeNull();
        _ynab.Verify(y => y.CreateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<YnabTransaction>()), Times.Never);
    }

    [Fact]
    public async Task LostResponse_RetryAfterReloadRecoversWithoutAnotherPost()
    {
        var fillUp = await SeedAsync();
        var id = fillUp.Id;
        _ynab.Setup(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()))
            .Returns((string _, string _, YnabTransaction _) =>
            {
                _transactions.Add(Remote(fillUp));
                return Task.FromException<YnabTransactionResult>(new TaskCanceledException("Response lost"));
            });
        await Service().PushAsync(fillUp);
        fillUp.YnabSyncStatus.Should().Be("pending");

        _db.ChangeTracker.Clear();
        var reloaded = await _db.FillUps.SingleAsync(f => f.Id == id);
        await Service().PushAsync(reloaded);

        reloaded.YnabSyncStatus.Should().Be("synced");
        reloaded.YnabTransactionId.Should().Be("remote-id");
        _ynab.Verify(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownOutcomeWithoutVisibleTransaction_DoesNotResend(bool serverError)
    {
        var fillUp = await SeedAsync();
        Exception error = serverError ? new HttpRequestException("Server error", null, HttpStatusCode.InternalServerError)
            : new TaskCanceledException("Timed out");
        _ynab.Setup(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>())).ThrowsAsync(error);

        await Service().PushAsync(fillUp);
        _db.ChangeTracker.Clear();
        var reloaded = await _db.FillUps.SingleAsync();
        await Service().PushAsync(reloaded);

        reloaded.YnabSyncStatus.Should().Be("pending");
        reloaded.YnabSyncError.Should().Contain("no new transaction was sent");
        _ynab.Verify(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()), Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ExplicitRejection_AllowsRetry(HttpStatusCode statusCode)
    {
        var fillUp = await SeedAsync();
        _ynab.SetupSequence(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()))
            .ThrowsAsync(new HttpRequestException("Rejected", null, statusCode))
            .ReturnsAsync(new YnabTransactionResult("remote-id", false));

        await Service().PushAsync(fillUp);
        fillUp.YnabSyncStatus.Should().Be("failed");
        await Service().PushAsync(fillUp);
        fillUp.YnabSyncStatus.Should().Be("synced");
        _ynab.Verify(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData("synced", null)]
    [InlineData("failed", "already-linked")]
    public async Task LinkedOrLegacySyncedFillUp_IsNotRecreated(string status, string? transactionId)
    {
        var fillUp = await SeedAsync(status, transactionId);
        await Service().PushAsync(fillUp);
        fillUp.YnabSyncStatus.Should().Be("synced");
        _ynab.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MultipleReferences_RequireResolutionWithoutPosting()
    {
        var fillUp = await SeedAsync("pending");
        _transactions.Add(Remote(fillUp));
        _transactions.Add(Remote(fillUp) with { Id = "duplicate-id" });
        await Service().PushAsync(fillUp);
        fillUp.YnabSyncStatus.Should().Be("pending");
        fillUp.YnabSyncError.Should().Contain("Multiple YNAB transactions");
        _ynab.Verify(y => y.CreateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<YnabTransaction>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingTransactionIdOrConflict_IsNotReportedAsSynced(bool duplicate)
    {
        var fillUp = await SeedAsync();
        _ynab.Setup(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()))
            .ReturnsAsync(new YnabTransactionResult(null, duplicate));
        await Service().PushAsync(fillUp);
        await Service().PushAsync(fillUp);
        fillUp.YnabSyncStatus.Should().Be("pending");
        fillUp.YnabTransactionId.Should().BeNull();
        _ynab.Verify(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()), Times.Once);
    }

    [Fact]
    public async Task LookupFailure_DoesNotSend()
    {
        var fillUp = await SeedAsync();
        _ynab.Setup(y => y.GetTransactionsAsync("token", "plan", null, null, null))
            .ThrowsAsync(new HttpRequestException("Unavailable"));
        await Service().PushAsync(fillUp);
        fillUp.YnabSyncStatus.Should().Be("failed");
        _ynab.Verify(y => y.CreateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<YnabTransaction>()), Times.Never);
    }

    [Fact]
    public async Task ConcurrentRequests_OnlyOneClaimsTheSend()
    {
        var fillUp = await SeedAsync();
        using var otherDb = new AppDbContext(_db.GetService<IDbContextOptions>()
            as DbContextOptions<AppDbContext> ?? throw new InvalidOperationException());
        var otherFillUp = await otherDb.FillUps.SingleAsync();
        _ynab.Setup(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()))
            .Returns(async (string _, string _, YnabTransaction _) =>
            {
                // This caller loaded the old state before the first caller persisted its claim.
                await Service(otherDb).PushAsync(otherFillUp);
                return new YnabTransactionResult("remote-id", false);
            });

        await Service().PushAsync(fillUp);

        fillUp.YnabSyncStatus.Should().Be("synced");
        otherFillUp.YnabSyncStatus.Should().Be("pending");
        _ynab.Verify(y => y.CreateTransactionAsync("token", "plan", It.IsAny<YnabTransaction>()), Times.Once);
    }

    [Fact]
    public async Task Pull_SkipsOwnTransactionsEvenBeforeResponseIsSaved()
    {
        var fillUp = await SeedAsync("pending");
        _transactions.Add(Remote(fillUp));
        _transactions.Add(Remote(fillUp, legacy: true) with { Id = "legacy-id" });

        var result = await new YnabPullSyncService(_db, _ynab.Object).PullAsync("token", "plan", categoryId: "category");

        result.Skipped.Should().Be(2);
        result.NewImports.Should().Be(0);
        _db.YnabImports.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
