using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Finance.Commands.CreateExchangeRate;

public sealed class CreateExchangeRateCommandValidator : AbstractValidator<CreateExchangeRateCommand>
{
    public CreateExchangeRateCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.CurrencyId)
            .MustAsync(async (currencyId, cancellationToken) =>
                await context.Currencies.AnyAsync(c => c.Id == currencyId, cancellationToken))
            .WithMessage("Para birimi bulunamadı.");

        RuleFor(x => x.RateToBase).GreaterThan(0);

        RuleFor(x => x)
            .MustAsync(async (command, cancellationToken) =>
                !await context.ExchangeRates.AnyAsync(
                    r => r.CurrencyId == command.CurrencyId && r.RateDate == command.RateDate,
                    cancellationToken))
            .WithMessage("Bu para birimi ve tarih için zaten bir kur kaydı mevcut.");
    }
}
