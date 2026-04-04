using FluentValidation.TestHelper;
using GasTracker.Api.Validators;
using GasTracker.Core.DTOs;

namespace GasTracker.Tests.Validators;

public class UpdateVehicleValidatorTests
{
    private readonly UpdateVehicleValidator _validator = new();

    [Fact]
    public void AllNull_IsValid()
    {
        var result = _validator.TestValidate(new UpdateVehicleRequest(null, null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Year_InRange_Passes()
    {
        var result = _validator.TestValidate(new UpdateVehicleRequest(2021, null, null, null, null));
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Year_OutOfRange_Fails()
    {
        var result = _validator.TestValidate(new UpdateVehicleRequest(1800, null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Make_Empty_WhenProvided_Fails()
    {
        var result = _validator.TestValidate(new UpdateVehicleRequest(null, "", null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.Make);
    }

    [Fact]
    public void Make_TooLong_Fails()
    {
        var result = _validator.TestValidate(new UpdateVehicleRequest(null, new string('X', 101), null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.Make);
    }

    [Fact]
    public void Model_Empty_WhenProvided_Fails()
    {
        var result = _validator.TestValidate(new UpdateVehicleRequest(null, null, "", null, null));
        result.ShouldHaveValidationErrorFor(x => x.Model);
    }

    [Fact]
    public void Make_Valid_WhenProvided_Passes()
    {
        var result = _validator.TestValidate(new UpdateVehicleRequest(null, "Honda", null, null, null));
        result.ShouldNotHaveValidationErrorFor(x => x.Make);
    }

    [Fact]
    public void OctaneRating_Valid_Passes()
    {
        var result = _validator.TestValidate(new UpdateVehicleRequest(null, null, null, null, 93));
        result.ShouldNotHaveValidationErrorFor(x => x.OctaneRating);
    }

    [Fact]
    public void OctaneRating_Invalid_Fails()
    {
        var result = _validator.TestValidate(new UpdateVehicleRequest(null, null, null, null, 88));
        result.ShouldHaveValidationErrorFor(x => x.OctaneRating);
    }
}
