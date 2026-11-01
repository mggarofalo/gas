using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GasTracker.Tests.Integration;

/// <summary>
/// Two households, each with a full set of data, and every endpoint exercised in both
/// directions: a caller sees and changes only its own household, and the other household's
/// resources answer 404 exactly as if they did not exist.
/// </summary>
public class HouseholdIsolationTests : IClassFixture<TestWebAppFactory>
{
    // Both households fill up at the same spot, so nearby results must be split by household.
    private const decimal Latitude = 40.712800m;
    private const decimal Longitude = -74.006000m;

    private readonly TestWebAppFactory _factory;

    public HouseholdIsolationTests(TestWebAppFactory factory) => _factory = factory;

    private sealed record Tenant(
        HttpClient Client,
        Guid HouseholdId,
        Guid VehicleId,
        Guid FillUpId,
        string ReceiptPath,
        string StationName,
        string YnabToken,
        string PlanName,
        Guid ImportId,
        Guid MappingId,
        string AccountName,
        string CategoryName);

    private async Task<(Tenant A, Tenant B)> TwoHouseholdsAsync()
    {
        var run = Guid.NewGuid().ToString("N")[..8];
        return (await SeedTenantAsync(run, "a"), await SeedTenantAsync(run, "b"));
    }

    private static IEnumerable<(Tenant Caller, Tenant Other)> BothDirections((Tenant A, Tenant B) pair)
    {
        yield return (pair.A, pair.B);
        yield return (pair.B, pair.A);
    }

    private async Task<Tenant> SeedTenantAsync(string run, string tag)
    {
        var (client, householdId) = await _factory.CreateClientInNewHouseholdAsync($"iso-{run}-{tag}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("YnabApiToken");

        var vehicle = new Vehicle { Id = Guid.NewGuid(), HouseholdId = householdId, Year = 2020, Make = "Make", Model = $"{run}-{tag}" };
        var stationName = $"Station {run} {tag}";
        var fillUpId = Guid.NewGuid();
        var receiptPath = $"{vehicle.Id}/{fillUpId}/receipt-{run}-{tag}.jpg";
        var fillUp = new FillUp
        {
            Id = fillUpId,
            Vehicle = vehicle,
            Date = new DateOnly(2026, 5, 1),
            OdometerMiles = 1000,
            Gallons = 10m,
            PricePerGallon = 3m,
            TotalCost = tag == "a" ? 30m : 45m,
            StationName = stationName,
            Latitude = Latitude,
            Longitude = Longitude,
            ReceiptPath = receiptPath,
        };
        var ynabToken = $"token-{run}-{tag}";
        var planName = $"Plan {run} {tag}";
        var import = new YnabImport
        {
            HouseholdId = householdId,
            YnabTransactionId = $"tx-{run}-{tag}",
            Date = new DateOnly(2026, 5, 2),
            PayeeName = stationName,
            AmountMilliunits = -30000,
            Gallons = 10m,
            PricePerGallon = 3m,
            OdometerMiles = 1300,
            VehicleId = vehicle.Id,
        };
        var mapping = new VehicleMemoMapping { HouseholdId = householdId, MemoName = $"memo-{run}-{tag}", Vehicle = vehicle };
        var accountName = $"Account {run} {tag}";
        var categoryName = $"Category {run} {tag}";

        db.Vehicles.Add(vehicle);
        db.FillUps.Add(fillUp);
        db.YnabSettings.Add(new YnabSettings
        {
            HouseholdId = householdId,
            ApiToken = protector.Protect(ynabToken),
            PlanId = $"plan-{run}-{tag}",
            PlanName = planName,
            AccountId = $"account-{run}-{tag}",
        });
        db.YnabImports.Add(import);
        db.VehicleMemoMappings.Add(mapping);
        db.YnabAccountCache.Add(new YnabAccountCache { HouseholdId = householdId, AccountId = $"account-{run}-{tag}", Name = accountName, Type = "checking" });
        db.YnabCategoryCache.Add(new YnabCategoryCache { HouseholdId = householdId, CategoryId = $"category-{run}-{tag}", Name = categoryName, CategoryGroupName = "Auto" });
        await db.SaveChangesAsync();

        return new Tenant(client, householdId, vehicle.Id, fillUpId, receiptPath, stationName,
            ynabToken, planName, import.Id, mapping.Id, accountName, categoryName);
    }

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = _factory.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private static MultipartFormDataContent ReceiptForm()
    {
        var file = new ByteArrayContent(new byte[64]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return new MultipartFormDataContent { { file, "receipt", "stolen.jpg" } };
    }

    // --- Auth ---

    [Fact]
    public async Task TokenWithoutHouseholdClaim_IsRejected()
    {
        var user = await _factory.SeedTestUserAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.GenerateTestToken(user, includeHousehold: false));

        (await client.GetAsync("/api/vehicles")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_OfATokenWithoutHouseholdClaim_IssuesOneWithTheClaim()
    {
        var email = $"iso-refresh-{Guid.NewGuid():N}@example.com";
        var user = await _factory.SeedUserAsync(email, TestWebAppFactory.TestUserPassword);
        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = TestWebAppFactory.TestUserPassword })).Content.ReadFromJsonAsync<JsonElement>();

        var refreshed = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            accessToken = _factory.GenerateTestToken(user, includeHousehold: false),
            refreshToken = login.GetProperty("refreshToken").GetString(),
        });

        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessToken = (await refreshed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Claims
            .Single(c => c.Type == HouseholdClaimTypes.HouseholdId).Value.Should().Be(user.HouseholdId.ToString());
    }

    // --- Vehicles ---

    [Fact]
    public async Task Vehicles_ListAndGet_ShowOnlyOwnHousehold()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            var list = await caller.Client.GetFromJsonAsync<JsonElement>("/api/vehicles?active=false");
            list.EnumerateArray().Select(v => v.GetProperty("id").GetGuid()).Should().Equal(caller.VehicleId);

            (await caller.Client.GetAsync($"/api/vehicles/{caller.VehicleId}")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await caller.Client.GetAsync($"/api/vehicles/{other.VehicleId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task Vehicles_WritesToAnotherHousehold_Return404AndChangeNothing()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            (await caller.Client.PutAsJsonAsync($"/api/vehicles/{other.VehicleId}", new { make = "Hijacked" }))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await caller.Client.DeleteAsync($"/api/vehicles/{other.VehicleId}"))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await caller.Client.PostAsync($"/api/vehicles/{other.VehicleId}/reactivate", null))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);

            var vehicle = await InDbAsync(db => db.Vehicles.SingleAsync(v => v.Id == other.VehicleId));
            vehicle.Make.Should().Be("Make");
            vehicle.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Vehicles_Create_IgnoresAClientSuppliedHousehold()
    {
        var (a, b) = await TwoHouseholdsAsync();

        var response = await a.Client.PostAsJsonAsync("/api/vehicles",
            new { year = (short)2021, make = "Spoof", model = "Attempt", householdId = b.HouseholdId });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await InDbAsync(db => db.Vehicles.SingleAsync(v => v.Id == id))).HouseholdId.Should().Be(a.HouseholdId);
    }

    // --- Fill-ups and receipts ---

    [Fact]
    public async Task FillUps_ListAndGet_ShowOnlyOwnHousehold()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            var page = await caller.Client.GetFromJsonAsync<JsonElement>("/api/fill-ups");
            page.GetProperty("totalCount").GetInt32().Should().Be(1);
            page.GetProperty("items").EnumerateArray().Select(f => f.GetProperty("id").GetGuid()).Should().Equal(caller.FillUpId);

            var filtered = await caller.Client.GetFromJsonAsync<JsonElement>($"/api/fill-ups?vehicleId={other.VehicleId}");
            filtered.GetProperty("totalCount").GetInt32().Should().Be(0);

            (await caller.Client.GetAsync($"/api/fill-ups/{caller.FillUpId}")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await caller.Client.GetAsync($"/api/fill-ups/{other.FillUpId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task FillUps_WritesToAnotherHousehold_Return404AndChangeNothing()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            var edit = new MultipartFormDataContent { { new StringContent("Hijacked"), "stationName" } };
            (await caller.Client.PutAsync($"/api/fill-ups/{other.FillUpId}", edit))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await caller.Client.PostAsync($"/api/fill-ups/{other.FillUpId}/resync", null))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await caller.Client.PostAsync($"/api/fill-ups/{other.FillUpId}/ynab-sync", null))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await caller.Client.DeleteAsync($"/api/fill-ups/{other.FillUpId}"))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);

            var fillUp = await InDbAsync(db => db.FillUps.SingleAsync(f => f.Id == other.FillUpId));
            fillUp.StationName.Should().Be(other.StationName);
            fillUp.PaperlessSyncStatus.Should().Be("none");
            fillUp.YnabSyncStatus.Should().Be("none");
        }
    }

    [Fact]
    public async Task FillUps_CreateAgainstAnotherHouseholdsVehicle_IsRejectedLikeAnUnknownVehicle()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            MultipartFormDataContent Form(Guid vehicleId) => new()
            {
                { new StringContent(vehicleId.ToString()), "vehicleId" },
                { new StringContent("2026-06-01"), "date" },
                { new StringContent("2000"), "odometerMiles" },
                { new StringContent("10"), "gallons" },
                { new StringContent("3.5"), "pricePerGallon" },
                { new StringContent("Intruder"), "stationName" },
            };

            var foreign = await caller.Client.PostAsync("/api/fill-ups", Form(other.VehicleId));
            var unknown = await caller.Client.PostAsync("/api/fill-ups", Form(Guid.NewGuid()));

            foreign.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await foreign.Content.ReadAsStringAsync()).Should().Be(await unknown.Content.ReadAsStringAsync());
            (await InDbAsync(db => db.FillUps.CountAsync(f => f.VehicleId == other.VehicleId))).Should().Be(1);
        }
    }

    [Fact]
    public async Task ReceiptProxy_DeniesAnotherHouseholdsReceipt()
    {
        var receiptStore = Mock.Get(_factory.Services.GetRequiredService<IReceiptStore>());
        var pair = await TwoHouseholdsAsync();

        foreach (var (caller, other) in BothDirections(pair))
        {
            (await caller.Client.GetAsync($"/api/fill-ups/{caller.FillUpId}/receipt")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await caller.Client.GetAsync($"/api/fill-ups/{other.FillUpId}/receipt")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await caller.Client.PutAsync($"/api/fill-ups/{other.FillUpId}/receipt", ReceiptForm()))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);

            receiptStore.Verify(r => r.DeleteAsync(other.ReceiptPath), Times.Never);
            receiptStore.Verify(r => r.UploadAsync(It.IsAny<Guid>(), other.FillUpId,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>()), Times.Never);
            (await InDbAsync(db => db.FillUps.SingleAsync(f => f.Id == other.FillUpId))).ReceiptPath.Should().Be(other.ReceiptPath);
        }

        // One download each: had either caller reached the other's object, that path would show two.
        receiptStore.Verify(r => r.DownloadAsync(pair.A.ReceiptPath), Times.Once);
        receiptStore.Verify(r => r.DownloadAsync(pair.B.ReceiptPath), Times.Once);
    }

    // --- Stats and locations ---

    [Fact]
    public async Task Stats_AggregateOnlyOwnHousehold()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            var own = await caller.Client.GetFromJsonAsync<JsonElement>("/api/stats");
            var ownCost = await InDbAsync(db => db.FillUps.Where(f => f.Id == caller.FillUpId).Select(f => f.TotalCost).SingleAsync());
            own.GetProperty("totalFillUps").GetInt32().Should().Be(1);
            own.GetProperty("totalCost").GetDecimal().Should().Be(ownCost);

            var foreign = await caller.Client.GetFromJsonAsync<JsonElement>($"/api/stats?vehicleId={other.VehicleId}");
            foreign.GetProperty("totalFillUps").GetInt32().Should().Be(0);
        }
    }

    [Fact]
    public async Task NearbyStations_ShowOnlyOwnHousehold()
    {
        foreach (var (caller, _) in BothDirections(await TwoHouseholdsAsync()))
        {
            var nearby = await caller.Client.GetFromJsonAsync<JsonElement>($"/api/locations/nearby?lat={Latitude}&lng={Longitude}");

            nearby.EnumerateArray().Select(s => s.GetProperty("stationName").GetString()).Should().Equal(caller.StationName);
        }
    }

    [Fact]
    public async Task StationSearch_SuggestsOnlyOwnHousehold()
    {
        var pair = await TwoHouseholdsAsync();
        var sharedPrefix = pair.A.StationName[..^2]; // "Station <run>" matches both households

        foreach (var (caller, _) in BothDirections(pair))
        {
            var results = await caller.Client.GetFromJsonAsync<JsonElement>($"/api/stations/search?q={Uri.EscapeDataString(sharedPrefix)}");

            results.EnumerateArray().Select(s => s.GetProperty("stationName").GetString()).Should().Equal(caller.StationName);
        }
    }

    // --- YNAB ---

    [Fact]
    public async Task YnabSettingsAndCaches_ShowOnlyOwnHousehold()
    {
        foreach (var (caller, _) in BothDirections(await TwoHouseholdsAsync()))
        {
            var settings = await caller.Client.GetFromJsonAsync<JsonElement>("/api/settings/ynab");
            settings.GetProperty("planName").GetString().Should().Be(caller.PlanName);

            var accounts = await caller.Client.GetFromJsonAsync<JsonElement>("/api/ynab/accounts/cached");
            accounts.EnumerateArray().Select(a => a.GetProperty("name").GetString()).Should().Equal(caller.AccountName);

            var categories = await caller.Client.GetFromJsonAsync<JsonElement>("/api/ynab/categories/cached");
            categories.EnumerateArray().Select(c => c.GetProperty("name").GetString()).Should().Equal(caller.CategoryName);
        }
    }

    [Fact]
    public async Task YnabProxy_UsesOnlyOwnHouseholdsToken()
    {
        var ynab = Mock.Get(_factory.Services.GetRequiredService<IYnabClient>());
        var pair = await TwoHouseholdsAsync();

        foreach (var (caller, _) in BothDirections(pair))
            (await caller.Client.GetAsync("/api/ynab/plans")).StatusCode.Should().Be(HttpStatusCode.OK);

        // One call each: had either caller used the other's token, that token would show two.
        ynab.Verify(y => y.GetPlansAsync(pair.A.YnabToken), Times.Once);
        ynab.Verify(y => y.GetPlansAsync(pair.B.YnabToken), Times.Once);
    }

    [Fact]
    public async Task YnabImports_ListShowsOnlyOwnHousehold_AndOtherImportsReturn404()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            var list = await caller.Client.GetFromJsonAsync<JsonElement>("/api/ynab/imports");
            list.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).Should().Equal(caller.ImportId);

            (await caller.Client.PutAsJsonAsync($"/api/ynab/imports/{other.ImportId}", new { gallons = 1m }))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await caller.Client.PostAsync($"/api/ynab/imports/{other.ImportId}/approve", null))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await caller.Client.DeleteAsync($"/api/ynab/imports/{other.ImportId}"))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);

            var import = await InDbAsync(db => db.YnabImports.SingleAsync(i => i.Id == other.ImportId));
            import.Status.Should().Be("pending");
            import.Gallons.Should().Be(10m);
        }
    }

    [Fact]
    public async Task YnabImports_CannotBePointedAtAnotherHouseholdsVehicle()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            var response = await caller.Client.PutAsJsonAsync($"/api/ynab/imports/{caller.ImportId}",
                new { gallons = 10m, pricePerGallon = 3m, odometerMiles = 1300, vehicleId = other.VehicleId });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await InDbAsync(db => db.YnabImports.SingleAsync(i => i.Id == caller.ImportId))).VehicleId.Should().Be(caller.VehicleId);
        }
    }

    [Fact]
    public async Task YnabImports_ApproveAllAndReset_TouchOnlyOwnHousehold()
    {
        var (a, b) = await TwoHouseholdsAsync();

        var approved = await (await a.Client.PostAsync("/api/ynab/imports/approve-all", null)).Content.ReadFromJsonAsync<JsonElement>();
        approved.GetProperty("approved").GetInt32().Should().Be(1);
        (await InDbAsync(db => db.YnabImports.SingleAsync(i => i.Id == b.ImportId))).Status.Should().Be("pending");

        var reset = await (await a.Client.PostAsync("/api/ynab/imports/reset", null)).Content.ReadFromJsonAsync<JsonElement>();
        reset.GetProperty("cleared").GetInt32().Should().Be(0);
        (await InDbAsync(db => db.YnabImports.AnyAsync(i => i.Id == b.ImportId))).Should().BeTrue();

        (await b.Client.PostAsync("/api/ynab/imports/reset", null)).EnsureSuccessStatusCode();
        (await InDbAsync(db => db.YnabImports.AnyAsync(i => i.Id == b.ImportId))).Should().BeFalse();
        (await InDbAsync(db => db.YnabImports.SingleAsync(i => i.Id == a.ImportId))).Status.Should().Be("approved");
    }

    [Fact]
    public async Task YnabBackfill_RejectsAnotherHouseholdsVehicles()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            var mapped = await caller.Client.PostAsJsonAsync("/api/admin/ynab-backfill",
                new { vehicleMappings = new Dictionary<string, Guid> { ["Car"] = other.VehicleId } });
            var defaulted = await caller.Client.PostAsJsonAsync("/api/admin/ynab-backfill",
                new { vehicleMappings = new Dictionary<string, Guid> { ["Car"] = caller.VehicleId }, defaultVehicleId = other.VehicleId });

            mapped.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await mapped.Content.ReadAsStringAsync()).Should().Contain("not found");
            defaulted.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await defaulted.Content.ReadAsStringAsync()).Should().Contain("not found");
        }
    }

    [Fact]
    public async Task MemoMappings_ShowAndChangeOnlyOwnHousehold()
    {
        foreach (var (caller, other) in BothDirections(await TwoHouseholdsAsync()))
        {
            var list = await caller.Client.GetFromJsonAsync<JsonElement>("/api/vehicle-memo-mappings");
            list.EnumerateArray().Select(m => m.GetProperty("id").GetGuid()).Should().Equal(caller.MappingId);

            (await caller.Client.PutAsJsonAsync("/api/vehicle-memo-mappings", new { memoName = "Stolen", vehicleId = other.VehicleId }))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await caller.Client.DeleteAsync($"/api/vehicle-memo-mappings/{other.MappingId}"))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);

            (await InDbAsync(db => db.VehicleMemoMappings.AnyAsync(m => m.Id == other.MappingId))).Should().BeTrue();
            (await InDbAsync(db => db.VehicleMemoMappings.AnyAsync(m => m.MemoName == "Stolen"))).Should().BeFalse();
        }
    }
}
