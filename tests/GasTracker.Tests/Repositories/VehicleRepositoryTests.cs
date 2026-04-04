using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Repositories;

namespace GasTracker.Tests.Repositories;

public class VehicleRepositoryTests
{
    private static Vehicle MakeVehicle(short year = 2021, string make = "Toyota", string model = "Tacoma", bool isActive = true)
        => new() { Id = Guid.NewGuid(), Year = year, Make = make, Model = model, IsActive = isActive };

    [Fact]
    public async Task ListAsync_ActiveOnly_ReturnsOnlyActive()
    {
        using var db = TestDbContextFactory.Create();
        db.Vehicles.AddRange(MakeVehicle(isActive: true), MakeVehicle(isActive: false));
        await db.SaveChangesAsync();
        var repo = new VehicleRepository(db);

        var result = await repo.ListAsync(activeOnly: true);

        result.Should().HaveCount(1);
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_AllVehicles_ReturnsBothActiveAndInactive()
    {
        using var db = TestDbContextFactory.Create();
        db.Vehicles.AddRange(MakeVehicle(isActive: true), MakeVehicle(isActive: false));
        await db.SaveChangesAsync();
        var repo = new VehicleRepository(db);

        var result = await repo.ListAsync(activeOnly: false);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAsync_OrdersByYearMakeModel()
    {
        using var db = TestDbContextFactory.Create();
        db.Vehicles.AddRange(
            MakeVehicle(2023, "Honda", "Civic"),
            MakeVehicle(2021, "Toyota", "Tacoma"),
            MakeVehicle(2021, "Honda", "Accord"));
        await db.SaveChangesAsync();
        var repo = new VehicleRepository(db);

        var result = await repo.ListAsync(false);

        result.Select(v => v.Label).Should().BeEquivalentTo(
            ["2021 Honda Accord", "2021 Toyota Tacoma", "2023 Honda Civic"],
            opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task GetByIdAsync_Exists_ReturnsVehicle()
    {
        using var db = TestDbContextFactory.Create();
        var vehicle = MakeVehicle();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        var repo = new VehicleRepository(db);

        var result = await repo.GetByIdAsync(vehicle.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(vehicle.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new VehicleRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_PersistsVehicle()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new VehicleRepository(db);
        var vehicle = MakeVehicle();

        var created = await repo.CreateAsync(vehicle);

        created.Id.Should().Be(vehicle.Id);
        db.Vehicles.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        using var db = TestDbContextFactory.Create();
        var vehicle = MakeVehicle();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        var repo = new VehicleRepository(db);

        vehicle.Notes = "Updated";
        await repo.UpdateAsync(vehicle);

        db.Vehicles.First().Notes.Should().Be("Updated");
    }
}
