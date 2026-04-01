using FluentValidation;
using GasTracker.Core.DTOs;

namespace GasTracker.Api.Validators;

public class CreateFillUpValidator : AbstractValidator<CreateFillUpRequest>
{
    public CreateFillUpValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.OdometerMiles).GreaterThan(0);
        RuleFor(x => x.Gallons).GreaterThan(0);
        RuleFor(x => x.PricePerGallon).GreaterThan(0);
        RuleFor(x => x.StationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StationAddress).MaximumLength(500);
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.Longitude)
            .NotNull().When(x => x.Latitude.HasValue)
            .WithMessage("Longitude required when latitude is provided");
        RuleFor(x => x.Latitude)
            .NotNull().When(x => x.Longitude.HasValue)
            .WithMessage("Latitude required when longitude is provided");
        RuleFor(x => x.OctaneRating)
            .Must(v => v is null or 87 or 89 or 91 or 93)
            .WithMessage("Octane must be 87, 89, 91, or 93");
    }
}
