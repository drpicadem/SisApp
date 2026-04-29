using FluentValidation;
using ŠišAppApi.Models.Requests;

namespace ŠišAppApi.Validators;

public class ServiceCategoryInsertValidator : AbstractValidator<ServiceCategoryInsertRequest>
{
    public ServiceCategoryInsertValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Naziv kategorije usluge je obavezan.");
    }
}
