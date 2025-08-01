using Application.DTOs.Cars;
using FluentValidation;

namespace Application.Validators.Cars;

public class CreateCarDtoValidator : AbstractValidator<CreateCarDto>
{
    public CreateCarDtoValidator()
    {
        RuleFor(x => x.Make)
            .NotEmpty().WithMessage("Make is required")
            .MaximumLength(100).WithMessage("Make must not exceed 100 characters");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required")
            .MaximumLength(100).WithMessage("Model must not exceed 100 characters");

        RuleFor(x => x.Year)
            .GreaterThan(1900).WithMessage("Year must be greater than 1900")
            .LessThanOrEqualTo(DateTime.Now.Year + 1).WithMessage("Year cannot be in the future");

        RuleFor(x => x.Vin)
            .NotEmpty().WithMessage("VIN is required")
            .Length(17).WithMessage("VIN must be exactly 17 characters");
    }
}

public class UpdateCarDtoValidator : AbstractValidator<UpdateCarDto>
{
    public UpdateCarDtoValidator()
    {
        RuleFor(x => x.Make)
            .NotEmpty().WithMessage("Make is required")
            .MaximumLength(100).WithMessage("Make must not exceed 100 characters");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required")
            .MaximumLength(100).WithMessage("Model must not exceed 100 characters");

        RuleFor(x => x.Year)
            .GreaterThan(1900).WithMessage("Year must be greater than 1900")
            .LessThanOrEqualTo(DateTime.Now.Year + 1).WithMessage("Year cannot be in the future");

        RuleFor(x => x.Vin)
            .NotEmpty().WithMessage("VIN is required")
            .Length(17).WithMessage("VIN must be exactly 17 characters");
    }
}