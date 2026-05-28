using FluentValidation;
using PetShop.Service.DTOs;

namespace PetShop.Service.Validators;

public class CreatePetRequestValidator : AbstractValidator<CreatePetRequest>
{
    public CreatePetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Breed).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AgeMonths).GreaterThanOrEqualTo(0).When(x => x.AgeMonths.HasValue);
        RuleFor(x => x.CategoryId).GreaterThan(0);
    }
}

public class UpdatePetRequestValidator : AbstractValidator<UpdatePetRequest>
{
    public UpdatePetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Breed).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AgeMonths).GreaterThanOrEqualTo(0).When(x => x.AgeMonths.HasValue);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.CategoryId).GreaterThan(0);
    }
}
