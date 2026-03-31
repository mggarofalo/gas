using FluentValidation.TestHelper;
using GasTracker.Api.Validators;
using GasTracker.Core.DTOs;

namespace GasTracker.Tests.Validators;

public class CreateFillUpValidatorTests
{
    private readonly CreateFillUpValidator _validator = new();

    private static CreateFillUpRequest ValidRequest() => new(
        VehicleId: Guid.NewGuid(),
        Date: "2026-03-28",
        OdometerMiles: 45000,
        Gallons: 14.5m,
        PricePerGallon: 3.299m,
        TotalCost: null,
        StationName: "Shell",
        StationAddress: null,
        Latitude: null,
        Longitude: null,
        Notes: null);

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void VehicleId_Empty_Fails()
    {
        var req = ValidRequest() with { VehicleId = Guid.Empty };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.VehicleId);
    }

    [Fact]
    public void Date_Empty_Fails()
    {
        var req = ValidRequest() with { Date = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OdometerMiles_NotPositive_Fails(int value)
    {
        var req = ValidRequest() with { OdometerMiles = value };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.OdometerMiles);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void Gallons_NotPositive_Fails(decimal value)
    {
        var req = ValidRequest() with { Gallons = value };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Gallons);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.0)]
    public void PricePerGallon_NotPositive_Fails(decimal value)
    {
        var req = ValidRequest() with { PricePerGallon = value };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.PricePerGallon);
    }

    [Fact]
    public void StationName_Empty_Fails()
    {
        var req = ValidRequest() with { StationName = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.StationName);
    }

    [Fact]
    public void StationName_TooLong_Fails()
    {
        var req = ValidRequest() with { StationName = new string('S', 201) };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.StationName);
    }

    [Fact]
    public void StationAddress_TooLong_Fails()
    {
        var req = ValidRequest() with { StationAddress = new string('A', 501) };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.StationAddress);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Latitude_OutOfRange_Fails(decimal lat)
    {
        var req = ValidRequest() with { Latitude = lat, Longitude = 0 };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(0)]
    [InlineData(90)]
    public void Latitude_InRange_Passes(decimal lat)
    {
        var req = ValidRequest() with { Latitude = lat, Longitude = 0 };
        _validator.TestValidate(req).ShouldNotHaveValidationErrorFor(x => x.Latitude);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Longitude_OutOfRange_Fails(decimal lng)
    {
        var req = ValidRequest() with { Latitude = 0, Longitude = lng };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void Latitude_WithoutLongitude_Fails()
    {
        var req = ValidRequest() with { Latitude = 33.749m, Longitude = null };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void Longitude_WithoutLatitude_Fails()
    {
        var req = ValidRequest() with { Latitude = null, Longitude = -84.388m };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Fact]
    public void BothCoords_Null_Passes()
    {
        var req = ValidRequest() with { Latitude = null, Longitude = null };
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
        result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void BothCoords_Present_Passes()
    {
        var req = ValidRequest() with { Latitude = 33.749m, Longitude = -84.388m };
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
        result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
    }
}
