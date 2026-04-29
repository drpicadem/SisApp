using FluentValidation;
using ŠišAppApi.Models.Requests;

namespace ŠišAppApi.Validators;

public class SalonAmenityUpdateValidator : AbstractValidator<SalonAmenityUpdateRequest>
{
    public SalonAmenityUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Naziv pogodnosti salona je obavezan.");
    }
}
