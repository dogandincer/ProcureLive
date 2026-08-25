using FluentValidation;

namespace ProcureLive.Application.Companies.Commands.UpdateCompany;

public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.TaxOffice).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Website).MaximumLength(250);
    }
}
