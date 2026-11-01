using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GasTracker.Tests.Integration;

/// <summary>
/// Writes that are keyed per household must not touch another household's rows.
/// Read and endpoint isolation is covered by <see cref="HouseholdIsolationTests"/>.
/// </summary>
public class HouseholdWriteTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public HouseholdWriteTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task YnabToken_IsStoredPerHousehold()
    {
        var (clientA, householdA) = await _factory.CreateClientInNewHouseholdAsync("ynab-a");
        var (clientB, householdB) = await _factory.CreateClientInNewHouseholdAsync("ynab-b");

        (await clientA.PutAsJsonAsync("/api/settings/ynab/token", new { apiToken = "token-a-1234" })).EnsureSuccessStatusCode();
        var tokenA = await StoredTokenAsync(householdA);
        (await clientB.PutAsJsonAsync("/api/settings/ynab/token", new { apiToken = "token-b-5678" })).EnsureSuccessStatusCode();

        (await StoredTokenAsync(householdA)).Should().Be(tokenA);
        (await StoredTokenAsync(householdB)).Should().NotBeNull().And.NotBe(tokenA);
    }

    [Fact]
    public async Task ImportList_ToleratesTheSameMemoNameInTwoHouseholds()
    {
        // The mapping PUT itself uses ExecuteUpdate, which the in-memory provider cannot run,
        // so the duplicate memo names are seeded directly.
        var (clientA, householdA) = await _factory.CreateClientInNewHouseholdAsync("memo-a");
        var (_, householdB) = await _factory.CreateClientInNewHouseholdAsync("memo-b");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.VehicleMemoMappings.AddRange(
                new VehicleMemoMapping { HouseholdId = householdA, MemoName = "Civic", VehicleId = await AddVehicleAsync(householdA) },
                new VehicleMemoMapping { HouseholdId = householdB, MemoName = "Civic", VehicleId = await AddVehicleAsync(householdB) });
            await db.SaveChangesAsync();
        }

        (await clientA.GetAsync("/api/ynab/imports")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> AddVehicleAsync(Guid householdId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vehicle = new Vehicle { HouseholdId = householdId, Year = 2016, Make = "Honda", Model = "Civic" };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle.Id;
    }

    private async Task<string?> StoredTokenAsync(Guid householdId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.YnabSettings.Where(s => s.HouseholdId == householdId).Select(s => s.ApiToken).SingleOrDefaultAsync();
    }
}
