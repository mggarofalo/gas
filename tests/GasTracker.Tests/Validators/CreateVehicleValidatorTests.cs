using FluentValidation.TestHelper;
using GasTracker.Api.Validators;
using GasTracker.Core.DTOs;

namespace GasTracker.Tests.Validators;

public class CreateVehicleValidatorTests
{
    private readonly CreateVehicleValidator _validator = new();

    private static CreateVehicleRequest ValidRequest(short? octane = null) =>
        new(2021, "Toyota", "Tacoma", null, octane);

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData((short)1899)]
    [InlineData((short)2101)]
    public void Year_OutOfRange_Fails(short year)
    {
        var result = _validator.TestValidate(ValidRequest() with { Year = year });
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Theory]
    [InlineData((short)1900)]
    [InlineData((short)2100)]
    public void Year_AtBoundary_Passes(short year)
    {
        var result = _validator.TestValidate(ValidRequest() with { Year = year });
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Make_EmptyOrNull_Fails(string? make)
    {
        var result = _validator.TestValidate(ValidRequest() with { Make = make! });
        result.ShouldHaveValidationErrorFor(x => x.Make);
    }

    [Fact]
    public void Make_TooLong_Fails()
    {
        var result = _validator.TestValidate(ValidRequest() with { Make = new string('A', 101) });
        result.ShouldHaveValidationErrorFor(x => x.Make);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Model_EmptyOrNull_Fails(string? model)
    {
        var result = _validator.TestValidate(ValidRequest() with { Model = model! });
        result.ShouldHaveValidationErrorFor(x => x.Model);
    }

    [Fact]
    public void Model_TooLong_Fails()
    {
        var result = _validator.TestValidate(ValidRequest() with { Model = new string('B', 101) });
        result.ShouldHaveValidationErrorFor(x => x.Model);
    }

    [Theory]
    [InlineData((short)87)]
    [InlineData((short)89)]
    [InlineData((short)91)]
    [InlineData((short)93)]
    public void OctaneRating_ValidValues_Passes(short octane)
    {
        var result = _validator.TestValidate(ValidRequest(octane));
        result.ShouldNotHaveValidationErrorFor(x => x.OctaneRating);
    }

    [Fact]
    public void OctaneRating_Null_Passes()
    {
        var result = _validator.TestValidate(ValidRequest(null));
        result.ShouldNotHaveValidationErrorFor(x => x.OctaneRating);
    }

    [Theory]
    [InlineData((short)88)]
    [InlineData((short)90)]
    [InlineData((short)95)]
    public void OctaneRating_InvalidValues_Fails(short octane)
    {
        var result = _validator.TestValidate(ValidRequest(octane));
        result.ShouldHaveValidationErrorFor(x => x.OctaneRating);
    }
}
