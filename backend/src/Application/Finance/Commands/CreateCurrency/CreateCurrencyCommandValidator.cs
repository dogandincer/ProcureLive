using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Finance.Commands.CreateCurrency;

public sealed class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(3)
            .MustAsync(async (code, cancellationToken) =>
                !await context.Currencies.AnyAsync(c => c.Code == code, cancellationToken))
            .WithMessage("Bu koda sahip bir para birimi zaten mevcut.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
    }
}
