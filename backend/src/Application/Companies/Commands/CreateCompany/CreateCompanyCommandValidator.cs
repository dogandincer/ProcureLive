using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);

        RuleFor(x => x.TaxNumber)
            .NotEmpty()
            .Length(10, 11)
            .MustAsync(async (taxNumber, cancellationToken) =>
                !await context.Companies.AnyAsync(c => c.TaxNumber == taxNumber, cancellationToken))
            .WithMessage("Bu vergi numarasına sahip cari zaten mevcut.");

        RuleFor(x => x.TaxOffice).NotEmpty().MaximumLength(150);

        RuleFor(x => x.CompanyType).IsInEnum();

        RuleFor(x => x.Website).MaximumLength(250);
    }
}
