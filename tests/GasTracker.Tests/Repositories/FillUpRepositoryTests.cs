using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Repositories;

namespace GasTracker.Tests.Repositories;

public class FillUpRepositoryTests
{
    private static readonly Guid VehicleId1 = Guid.NewGuid();
    private static readonly Guid VehicleId2 = Guid.NewGuid();

    private static Vehicle MakeVehicle(Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), Year = 2021, Make = "Toyota", Model = "Tacoma" };

    private static FillUp MakeFillUp(
        Guid? vehicleId = null, DateOnly? date = null, int odometer = 45000,
        decimal gallons = 14.5m, decimal price = 3.299m, string station = "Shell")
        => new()
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId ?? VehicleId1,
            Date = date ?? new DateOnly(2026, 3, 28),
            OdometerMiles = odometer,
            Gallons = gallons,
            PricePerGallon = price,
            TotalCost = Math.Round(gallons * price, 2),
            StationName = station,
        };

    private async Task<GasTracker.Infrastructure.Data.AppDbContext> SeedDb(params FillUp[] fillUps)
    {
        var db = TestDbContextFactory.Create();
        db.Vehicles.AddRange(MakeVehicle(VehicleId1), MakeVehicle(VehicleId2));
        db.FillUps.AddRange(fillUps);
        await db.SaveChangesAsync();
        return db;
    }

    // --- ListAsync ---

    [Fact]
    public async Task ListAsync_NoFilters_ReturnsAll()
    {
        var db = await SeedDb(MakeFillUp(), MakeFillUp(date: new DateOnly(2026, 3, 27)));
        var repo = new FillUpRepository(db);

        var (items, total) = await repo.ListAsync(null, null, null, 1, 25, "date", "desc");

        items.Should().HaveCount(2);
        total.Should().Be(2);
    }

    [Fact]
    public async Task ListAsync_FilterByVehicle()
    {
        var db = await SeedDb(
            MakeFillUp(VehicleId1),
            MakeFillUp(VehicleId2));
        var repo = new FillUpRepository(db);

        var (items, total) = await repo.ListAsync(VehicleId1, null, null, 1, 25, "date", "desc");

        items.Should().HaveCount(1);
        items[0].VehicleId.Should().Be(VehicleId1);
    }

    [Fact]
    public async Task ListAsync_FilterByDateRange()
    {
        var db = await SeedDb(
            MakeFillUp(date: new DateOnly(2026, 1, 1)),
            MakeFillUp(date: new DateOnly(2026, 6, 15)),
            MakeFillUp(date: new DateOnly(2026, 12, 31)));
        var repo = new FillUpRepository(db);

        var (items, _) = await repo.ListAsync(null, new DateOnly(2026, 3, 1), new DateOnly(2026, 9, 1), 1, 25, "date", "desc");

        items.Should().HaveCount(1);
        items[0].Date.Should().Be(new DateOnly(2026, 6, 15));
    }

    [Fact]
    public async Task ListAsync_Pagination_ReturnsCorrectPage()
    {
        var fillUps = Enumerable.Range(1, 10)
            .Select(i => MakeFillUp(date: new DateOnly(2026, 1, i)))
            .ToArray();
        var db = await SeedDb(fillUps);
        var repo = new FillUpRepository(db);

        var (items, total) = await repo.ListAsync(null, null, null, 2, 3, "date", "asc");

        total.Should().Be(10);
        items.Should().HaveCount(3);
        items[0].Date.Should().Be(new DateOnly(2026, 1, 4)); // page 2, 3 items/page
    }

    [Fact]
    public async Task ListAsync_SortByDate_Desc()
    {
        var db = await SeedDb(
            MakeFillUp(date: new DateOnly(2026, 1, 1)),
            MakeFillUp(date: new DateOnly(2026, 3, 1)));
        var repo = new FillUpRepository(db);

        var (items, _) = await repo.ListAsync(null, null, null, 1, 25, "date", "desc");

        items[0].Date.Should().Be(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public async Task ListAsync_SortByDate_Asc()
    {
        var db = await SeedDb(
            MakeFillUp(date: new DateOnly(2026, 3, 1)),
            MakeFillUp(date: new DateOnly(2026, 1, 1)));
        var repo = new FillUpRepository(db);

        var (items, _) = await repo.ListAsync(null, null, null, 1, 25, "date", "asc");

        items[0].Date.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public async Task ListAsync_SortByGallons_Desc()
    {
        var db = await SeedDb(
            MakeFillUp(gallons: 10),
            MakeFillUp(gallons: 20));
        var repo = new FillUpRepository(db);

        var (items, _) = await repo.ListAsync(null, null, null, 1, 25, "gallons", "desc");

        items[0].Gallons.Should().Be(20);
    }

    [Fact]
    public async Task ListAsync_SortByTotal_Asc()
    {
        var db = await SeedDb(
            MakeFillUp(gallons: 20, price: 3),
            MakeFillUp(gallons: 5, price: 3));
        var repo = new FillUpRepository(db);

        var (items, _) = await repo.ListAsync(null, null, null, 1, 25, "total", "asc");

        items[0].TotalCost.Should().BeLessThan(items[1].TotalCost);
    }

    [Fact]
    public async Task ListAsync_DefaultSort_IsDateDesc()
    {
        var db = await SeedDb(
            MakeFillUp(date: new DateOnly(2026, 1, 1)),
            MakeFillUp(date: new DateOnly(2026, 3, 1)));
        var repo = new FillUpRepository(db);

        var (items, _) = await repo.ListAsync(null, null, null, 1, 25, "unknown", "whatever");

        items[0].Date.Should().Be(new DateOnly(2026, 3, 1));
    }

    // --- GetTripMilesAsync ---

    [Fact]
    public async Task GetTripMilesAsync_FirstFillUp_ReturnsNull()
    {
        var fillUp = MakeFillUp(odometer: 45000);
        var db = await SeedDb(fillUp);
        var repo = new FillUpRepository(db);

        var result = await repo.GetTripMilesAsync(fillUp);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTripMilesAsync_WithPriorFillUp_ReturnsDifference()
    {
        var first = MakeFillUp(date: new DateOnly(2026, 3, 1), odometer: 44000);
        var second = MakeFillUp(date: new DateOnly(2026, 3, 15), odometer: 44350);
        var db = await SeedDb(first, second);
        var repo = new FillUpRepository(db);

        var result = await repo.GetTripMilesAsync(second);

        result.Should().Be(350);
    }

    [Fact]
    public async Task GetTripMilesAsync_SameDate_UsesOdometerOrdering()
    {
        var first = MakeFillUp(date: new DateOnly(2026, 3, 1), odometer: 44000);
        var second = MakeFillUp(date: new DateOnly(2026, 3, 1), odometer: 44100);
        var db = await SeedDb(first, second);
        var repo = new FillUpRepository(db);

        var result = await repo.GetTripMilesAsync(second);

        result.Should().Be(100);
    }

    [Fact]
    public async Task GetTripMilesAsync_DifferentVehicle_NotIncluded()
    {
        var v1Fill = MakeFillUp(VehicleId1, new DateOnly(2026, 3, 1), 44000);
        var v2Fill = MakeFillUp(VehicleId2, new DateOnly(2026, 3, 15), 44350);
        var db = await SeedDb(v1Fill, v2Fill);
        var repo = new FillUpRepository(db);

        var result = await repo.GetTripMilesAsync(v2Fill);

        result.Should().BeNull(); // No prior fill-up for vehicle 2
    }

    [Fact]
    public async Task GetTripMilesAsync_MultiplePrior_UsesClosest()
    {
        var first = MakeFillUp(date: new DateOnly(2026, 1, 1), odometer: 43000);
        var second = MakeFillUp(date: new DateOnly(2026, 2, 1), odometer: 43500);
        var third = MakeFillUp(date: new DateOnly(2026, 3, 1), odometer: 44200);
        var db = await SeedDb(first, second, third);
        var repo = new FillUpRepository(db);

        var result = await repo.GetTripMilesAsync(third);

        result.Should().Be(700); // 44200 - 43500
    }

    // --- GetStatsAsync ---

    [Fact]
    public async Task GetStatsAsync_NoFillUps_ReturnsZeros()
    {
        var db = await SeedDb();
        var repo = new FillUpRepository(db);

        var stats = await repo.GetStatsAsync(null, null, null);

        stats.TotalFillUps.Should().Be(0);
        stats.TotalGallons.Should().Be(0);
        stats.TotalCost.Should().Be(0);
        stats.TotalMiles.Should().Be(0);
        stats.AvgMpg.Should().BeNull();
        stats.CostPerMile.Should().BeNull();
    }

    [Fact]
    public async Task GetStatsAsync_SingleFillUp_ZeroMiles()
    {
        var db = await SeedDb(MakeFillUp(odometer: 45000, gallons: 14.5m, price: 3.0m));
        var repo = new FillUpRepository(db);

        var stats = await repo.GetStatsAsync(null, null, null);

        stats.TotalFillUps.Should().Be(1);
        stats.TotalGallons.Should().Be(14.5m);
        stats.TotalMiles.Should().Be(0); // Only one fill-up -> no range
        stats.AvgMpg.Should().BeNull();
        stats.CostPerMile.Should().BeNull();
    }

    [Fact]
    public async Task GetStatsAsync_MultipleFillUps_ComputesCorrectly()
    {
        var db = await SeedDb(
            MakeFillUp(VehicleId1, new DateOnly(2026, 1, 1), 40000, 15, 3.0m),
            MakeFillUp(VehicleId1, new DateOnly(2026, 2, 1), 40300, 15, 3.5m));
        var repo = new FillUpRepository(db);

        var stats = await repo.GetStatsAsync(null, null, null);

        stats.TotalFillUps.Should().Be(2);
        stats.TotalGallons.Should().Be(30);
        stats.TotalMiles.Should().Be(300); // 40300 - 40000
        stats.AvgMpg.Should().Be(10.0m); // 300 / 30
        stats.CostPerMile.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatsAsync_MultiVehicle_SumsMilesPerVehicle()
    {
        var db = await SeedDb(
            MakeFillUp(VehicleId1, new DateOnly(2026, 1, 1), 40000, 10, 3.0m),
            MakeFillUp(VehicleId1, new DateOnly(2026, 2, 1), 40500, 10, 3.0m),
            MakeFillUp(VehicleId2, new DateOnly(2026, 1, 1), 20000, 10, 3.0m),
            MakeFillUp(VehicleId2, new DateOnly(2026, 2, 1), 20200, 10, 3.0m));
        var repo = new FillUpRepository(db);

        var stats = await repo.GetStatsAsync(null, null, null);

        stats.TotalMiles.Should().Be(700); // 500 (v1) + 200 (v2)
        stats.TotalGallons.Should().Be(40);
    }

    [Fact]
    public async Task GetStatsAsync_FilterByVehicle()
    {
        var db = await SeedDb(
            MakeFillUp(VehicleId1, gallons: 10),
            MakeFillUp(VehicleId2, gallons: 20));
        var repo = new FillUpRepository(db);

        var stats = await repo.GetStatsAsync(VehicleId1, null, null);

        stats.TotalFillUps.Should().Be(1);
        stats.TotalGallons.Should().Be(10);
    }

    [Fact]
    public async Task GetStatsAsync_FilterByDateRange()
    {
        var db = await SeedDb(
            MakeFillUp(date: new DateOnly(2026, 1, 1)),
            MakeFillUp(date: new DateOnly(2026, 6, 1)),
            MakeFillUp(date: new DateOnly(2026, 12, 1)));
        var repo = new FillUpRepository(db);

        var stats = await repo.GetStatsAsync(null, new DateOnly(2026, 3, 1), new DateOnly(2026, 9, 1));

        stats.TotalFillUps.Should().Be(1);
    }

    // --- CRUD ---

    [Fact]
    public async Task CreateAsync_PersistsFillUp()
    {
        var db = await SeedDb();
        var repo = new FillUpRepository(db);
        var fillUp = MakeFillUp();

        var created = await repo.CreateAsync(fillUp);

        created.Id.Should().Be(fillUp.Id);
        db.FillUps.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFillUp()
    {
        var fillUp = MakeFillUp();
        var db = await SeedDb(fillUp);
        var repo = new FillUpRepository(db);

        await repo.DeleteAsync(fillUp);

        db.FillUps.Should().BeEmpty();
    }
}
