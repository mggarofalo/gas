using FluentAssertions;
using GasTracker.Core.Entities;

namespace GasTracker.Tests.Data;

public class AppDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_AddedVehicle_SetsCreatedAtAndUpdatedAt()
    {
        using var db = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Year = 2021, Make = "Toyota", Model = "Tacoma" };
        db.Vehicles.Add(vehicle);

        await db.SaveChangesAsync();

        vehicle.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        vehicle.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        vehicle.CreatedAt.Should().Be(vehicle.UpdatedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedVehicle_UpdatesOnlyUpdatedAt()
    {
        using var db = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Year = 2021, Make = "Toyota", Model = "Tacoma" };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var originalCreated = vehicle.CreatedAt;
        await Task.Delay(50); // Small delay to ensure different timestamps

        vehicle.Notes = "Updated";
        db.Vehicles.Update(vehicle);
        await db.SaveChangesAsync();

        vehicle.CreatedAt.Should().Be(originalCreated); // Unchanged
        vehicle.UpdatedAt.Should().BeOnOrAfter(originalCreated);
    }

    [Fact]
    public async Task SaveChangesAsync_AddedFillUp_SetsTimestamps()
    {
        using var db = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Year = 2021, Make = "Toyota", Model = "Tacoma" };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var fillUp = new FillUp
        {
            VehicleId = vehicle.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            OdometerMiles = 45000,
            Gallons = 14.5m,
            PricePerGallon = 3.299m,
            TotalCost = 47.84m,
            StationName = "Shell",
        };
        db.FillUps.Add(fillUp);
        await db.SaveChangesAsync();

        fillUp.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        fillUp.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedFillUp_UpdatesUpdatedAt()
    {
        using var db = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Year = 2021, Make = "Toyota", Model = "Tacoma" };
        db.Vehicles.Add(vehicle);
        var fillUp = new FillUp
        {
            VehicleId = vehicle.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            OdometerMiles = 45000,
            Gallons = 14.5m,
            PricePerGallon = 3.299m,
            TotalCost = 47.84m,
            StationName = "Shell",
        };
        db.FillUps.Add(fillUp);
        await db.SaveChangesAsync();

        var originalCreated = fillUp.CreatedAt;
        await Task.Delay(50);

        fillUp.Notes = "Updated note";
        db.FillUps.Update(fillUp);
        await db.SaveChangesAsync();

        fillUp.CreatedAt.Should().Be(originalCreated);
        fillUp.UpdatedAt.Should().BeOnOrAfter(originalCreated);
    }

    [Fact]
    public void SaveChanges_Sync_SetsTimestamps()
    {
        using var db = TestDbContextFactory.Create();
        var vehicle = new Vehicle { Year = 2021, Make = "Toyota", Model = "Tacoma" };
        db.Vehicles.Add(vehicle);

        db.SaveChanges();

        vehicle.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        vehicle.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task SaveChangesAsync_MultipleEntities_AllTimestamped()
    {
        using var db = TestDbContextFactory.Create();
        var v1 = new Vehicle { Year = 2021, Make = "Toyota", Model = "Tacoma" };
        var v2 = new Vehicle { Year = 2023, Make = "Honda", Model = "Civic" };
        db.Vehicles.AddRange(v1, v2);

        await db.SaveChangesAsync();

        v1.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        v2.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        // Both should have the same timestamp (set in same save batch)
        v1.CreatedAt.Should().Be(v2.CreatedAt);
    }
}
