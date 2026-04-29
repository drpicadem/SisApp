using FluentValidation;
using ŠišAppApi.Models.Requests;

namespace ŠišAppApi.Validators;

public class ServiceCategoryUpdateValidator : AbstractValidator<ServiceCategoryUpdateRequest>
{
    public ServiceCategoryUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Naziv kategorije usluge je obavezan.");
    }
}
