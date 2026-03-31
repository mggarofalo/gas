using FluentAssertions;
using GasTracker.Api;
using GasTracker.Core.Entities;

namespace GasTracker.Tests.Mappings;

public class VehicleMappingTests
{
    private static Vehicle MakeVehicle(
        short year = 2021, string make = "Toyota", string model = "Tacoma",
        bool isActive = true, string? notes = null)
    {
        var v = new Vehicle { Year = year, Make = make, Model = model, IsActive = isActive, Notes = notes };
        v.CreatedAt = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
        v.UpdatedAt = new DateTimeOffset(2026, 3, 20, 14, 30, 0, TimeSpan.Zero);
        return v;
    }

    [Fact]
    public void ToDto_MapsAllFields()
    {
        var v = MakeVehicle(notes: "Test notes");
        v.Id = Guid.NewGuid();

        var dto = v.ToDto();

        dto.Id.Should().Be(v.Id);
        dto.Year.Should().Be(2021);
        dto.Make.Should().Be("Toyota");
        dto.Model.Should().Be("Tacoma");
        dto.Notes.Should().Be("Test notes");
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().Be(v.CreatedAt);
        dto.UpdatedAt.Should().Be(v.UpdatedAt);
    }

    [Fact]
    public void ToDto_Label_ConcatenatesYearMakeModel()
    {
        var dto = MakeVehicle(2023, "Land Rover", "Range Rover Sport").ToDto();
        dto.Label.Should().Be("2023 Land Rover Range Rover Sport");
    }

    [Fact]
    public void ToDto_InactiveVehicle_ReflectsIsActive()
    {
        var dto = MakeVehicle(isActive: false).ToDto();
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ToDto_NullNotes_MapsAsNull()
    {
        var dto = MakeVehicle(notes: null).ToDto();
        dto.Notes.Should().BeNull();
    }
}

public class FillUpMappingTests
{
    private static Vehicle MakeVehicle() =>
        new() { Id = Guid.NewGuid(), Year = 2021, Make = "Toyota", Model = "Tacoma" };

    private static FillUp MakeFillUp(Vehicle? vehicle = null, string? receiptPath = null)
    {
        var v = vehicle ?? MakeVehicle();
        return new FillUp
        {
            Id = Guid.NewGuid(),
            VehicleId = v.Id,
            Vehicle = v,
            Date = new DateOnly(2026, 3, 28),
            OdometerMiles = 45000,
            Gallons = 14.5m,
            PricePerGallon = 3.299m,
            TotalCost = 47.84m,
            StationName = "Shell",
            StationAddress = "123 Main St",
            Latitude = 33.749m,
            Longitude = -84.388m,
            ReceiptPath = receiptPath,
            PaperlessSyncStatus = "none",
            Notes = "Test fill-up",
            CreatedAt = new DateTimeOffset(2026, 3, 28, 12, 0, 0, TimeSpan.Zero),
        };
    }

    // --- MPG computation ---

    [Fact]
    public void ToDto_Mpg_PositiveTripMilesAndGallons_ComputesCorrectly()
    {
        var f = MakeFillUp();
        f.Gallons = 10m;
        var dto = f.ToDto(tripMiles: 300);
        dto.Mpg.Should().Be(30.00m); // 300 / 10
    }

    [Fact]
    public void ToDto_Mpg_NullTripMiles_ReturnsNull()
    {
        var dto = MakeFillUp().ToDto(tripMiles: null);
        dto.Mpg.Should().BeNull();
    }

    [Fact]
    public void ToDto_Mpg_ZeroTripMiles_ReturnsNull()
    {
        var dto = MakeFillUp().ToDto(tripMiles: 0);
        dto.Mpg.Should().BeNull();
    }

    [Fact]
    public void ToDto_Mpg_ZeroGallons_ReturnsNull()
    {
        var f = MakeFillUp();
        f.Gallons = 0m;
        var dto = f.ToDto(tripMiles: 300);
        dto.Mpg.Should().BeNull();
    }

    [Fact]
    public void ToDto_Mpg_RoundsToTwoDecimals()
    {
        var f = MakeFillUp();
        f.Gallons = 13.7m;
        var dto = f.ToDto(tripMiles: 350);
        // 350 / 13.7 = 25.5474... -> 25.55
        dto.Mpg.Should().Be(25.55m);
    }

    [Fact]
    public void ToDto_Mpg_SmallTripMiles_StillComputes()
    {
        var f = MakeFillUp();
        f.Gallons = 2m;
        var dto = f.ToDto(tripMiles: 1);
        dto.Mpg.Should().Be(0.50m); // 1 / 2
    }

    // --- CostPerMile computation ---

    [Fact]
    public void ToDto_CostPerMile_PositiveTripMiles_ComputesCorrectly()
    {
        var f = MakeFillUp();
        f.TotalCost = 45.00m;
        var dto = f.ToDto(tripMiles: 300);
        dto.CostPerMile.Should().Be(0.15m); // 45 / 300
    }

    [Fact]
    public void ToDto_CostPerMile_NullTripMiles_ReturnsNull()
    {
        var dto = MakeFillUp().ToDto(tripMiles: null);
        dto.CostPerMile.Should().BeNull();
    }

    [Fact]
    public void ToDto_CostPerMile_ZeroTripMiles_ReturnsNull()
    {
        var dto = MakeFillUp().ToDto(tripMiles: 0);
        dto.CostPerMile.Should().BeNull();
    }

    [Fact]
    public void ToDto_CostPerMile_RoundsToTwoDecimals()
    {
        var f = MakeFillUp();
        f.TotalCost = 47.84m;
        var dto = f.ToDto(tripMiles: 350);
        // 47.84 / 350 = 0.1366... -> 0.14
        dto.CostPerMile.Should().Be(0.14m);
    }

    // --- Receipt URL construction ---

    [Fact]
    public void ToDto_ReceiptUrl_WhenReceiptPathIsNull_ReturnsNull()
    {
        var dto = MakeFillUp(receiptPath: null).ToDto(tripMiles: null);
        dto.ReceiptUrl.Should().BeNull();
    }

    [Fact]
    public void ToDto_ReceiptUrl_WhenReceiptPathIsSet_ReturnsCorrectUrl()
    {
        var f = MakeFillUp(receiptPath: "some/path/receipt.jpg");
        var dto = f.ToDto(tripMiles: null);
        dto.ReceiptUrl.Should().Be($"/api/fill-ups/{f.Id}/receipt");
    }

    // --- Vehicle label mapping ---

    [Fact]
    public void ToDto_VehicleLabel_WhenVehiclePresent_ReturnsLabel()
    {
        var vehicle = new Vehicle { Year = 2023, Make = "Honda", Model = "Civic" };
        var dto = MakeFillUp(vehicle).ToDto(tripMiles: null);
        dto.VehicleLabel.Should().Be("2023 Honda Civic");
    }

    [Fact]
    public void ToDto_VehicleLabel_WhenVehicleIsNull_ReturnsEmpty()
    {
        var f = MakeFillUp();
        f.Vehicle = null!;
        var dto = f.ToDto(tripMiles: null);
        dto.VehicleLabel.Should().Be("");
    }

    // --- Other field mappings ---

    [Fact]
    public void ToDto_MapsAllScalarFields()
    {
        var f = MakeFillUp(receiptPath: "path/to/receipt.jpg");
        var dto = f.ToDto(tripMiles: 350);

        dto.Id.Should().Be(f.Id);
        dto.VehicleId.Should().Be(f.VehicleId);
        dto.Date.Should().Be("2026-03-28");
        dto.OdometerMiles.Should().Be(45000);
        dto.Gallons.Should().Be(14.5m);
        dto.PricePerGallon.Should().Be(3.299m);
        dto.TotalCost.Should().Be(47.84m);
        dto.StationName.Should().Be("Shell");
        dto.StationAddress.Should().Be("123 Main St");
        dto.Latitude.Should().Be(33.749m);
        dto.Longitude.Should().Be(-84.388m);
        dto.TripMiles.Should().Be(350);
        dto.PaperlessSyncStatus.Should().Be("none");
        dto.Notes.Should().Be("Test fill-up");
    }

    [Fact]
    public void ToDto_CreatedAt_FormattedAsIso8601()
    {
        var f = MakeFillUp();
        var dto = f.ToDto(tripMiles: null);
        dto.CreatedAt.Should().Be(f.CreatedAt.ToString("o"));
    }

    [Fact]
    public void ToDto_NullOptionalFields_MapAsNull()
    {
        var f = MakeFillUp();
        f.StationAddress = null;
        f.Latitude = null;
        f.Longitude = null;
        f.Notes = null;
        var dto = f.ToDto(tripMiles: null);

        dto.StationAddress.Should().BeNull();
        dto.Latitude.Should().BeNull();
        dto.Longitude.Should().BeNull();
        dto.Notes.Should().BeNull();
    }
}
