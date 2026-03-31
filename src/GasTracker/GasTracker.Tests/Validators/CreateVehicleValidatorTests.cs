using FluentAssertions;
using FluentValidation.TestHelper;
using GasTracker.Api.Validators;
using GasTracker.Core.DTOs;

namespace GasTracker.Tests.Validators;

public class CreateVehicleValidatorTests
{
    private readonly CreateVehicleValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.TestValidate(new CreateVehicleRequest(2021, "Toyota", "Tacoma", null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData((short)1899)]
    [InlineData((short)2101)]
    public void Year_OutOfRange_Fails(short year)
    {
        var result = _validator.TestValidate(new CreateVehicleRequest(year, "Toyota", "Tacoma", null));
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Theory]
    [InlineData((short)1900)]
    [InlineData((short)2100)]
    public void Year_AtBoundary_Passes(short year)
    {
        var result = _validator.TestValidate(new CreateVehicleRequest(year, "Toyota", "Tacoma", null));
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Make_EmptyOrNull_Fails(string? make)
    {
        var result = _validator.TestValidate(new CreateVehicleRequest(2021, make!, "Tacoma", null));
        result.ShouldHaveValidationErrorFor(x => x.Make);
    }

    [Fact]
    public void Make_TooLong_Fails()
    {
        var result = _validator.TestValidate(new CreateVehicleRequest(2021, new string('A', 101), "Tacoma", null));
        result.ShouldHaveValidationErrorFor(x => x.Make);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Model_EmptyOrNull_Fails(string? model)
    {
        var result = _validator.TestValidate(new CreateVehicleRequest(2021, "Toyota", model!, null));
        result.ShouldHaveValidationErrorFor(x => x.Model);
    }

    [Fact]
    public void Model_TooLong_Fails()
    {
        var result = _validator.TestValidate(new CreateVehicleRequest(2021, "Toyota", new string('B', 101), null));
        result.ShouldHaveValidationErrorFor(x => x.Model);
    }
}
