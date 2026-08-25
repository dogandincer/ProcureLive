using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Finance;

namespace ProcureLive.Application.Common.Currencies;

/// <summary>
/// Shared cross-currency comparison helper. Used wherever amounts placed in different currencies
/// must be compared side by side: Auction bids ("farklı para birimleriyle verilmiş olsa bile
/// kıyaslanabilir") and RFQ quotes ("çoklu para biriminde teklif toplayabilme"). Normalizes to the
/// system base currency using the latest known exchange rate.
/// </summary>
public static class CurrencyNormalizer
{
    public static async Task<decimal> NormalizeToBaseCurrencyAsync(
        IApplicationDbContext context,
        decimal amount,
        Guid currencyId,
        CancellationToken cancellationToken)
    {
        Currency? currency = await context.Currencies.FirstOrDefaultAsync(c => c.Id == currencyId, cancellationToken);

        if (currency is null)
        {
            throw new NotFoundException(nameof(Currency), currencyId);
        }

        if (currency.IsBaseCurrency)
        {
            return amount;
        }

        ExchangeRate? latestRate = await context.ExchangeRates
            .Where(r => r.CurrencyId == currencyId)
            .OrderByDescending(r => r.RateDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestRate is null)
        {
            throw new ConflictException($"'{currency.Code}' para birimi için güncel bir kur tanımlı değil.");
        }

        return amount * latestRate.RateToBase;
    }
}
