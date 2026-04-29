using FluentValidation;
using ŠišAppApi.Models.Requests;

namespace ŠišAppApi.Validators;

public class SalonAmenityInsertValidator : AbstractValidator<SalonAmenityInsertRequest>
{
    public SalonAmenityInsertValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Naziv pogodnosti salona je obavezan.");
    }
}
