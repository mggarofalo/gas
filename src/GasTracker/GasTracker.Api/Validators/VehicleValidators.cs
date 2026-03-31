using FluentValidation;
using GasTracker.Core.DTOs;

namespace GasTracker.Api.Validators;

public class CreateVehicleValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween((short)1900, (short)2100);
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
    }
}

public class UpdateVehicleValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween((short)1900, (short)2100).When(x => x.Year.HasValue);
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100).When(x => x.Make is not null);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100).When(x => x.Model is not null);
    }
}
