namespace ProcureLive.Application.Common.Interfaces;

public sealed record ExternalExchangeRate(string CurrencyCode, decimal RateToBase);

/// <summary>
/// Abstraction over an external exchange-rate source (Infrastructure ships a TCMB/Turkish Central Bank
/// implementation). RateToBase follows the same convention as ExchangeRate.RateToBase: the price of
/// 1 unit of CurrencyCode expressed in the system's base currency (TRY).
/// </summary>
public interface IExchangeRateProvider
{
    string SourceName { get; }

    Task<IReadOnlyCollection<ExternalExchangeRate>> GetLatestRatesAsync(CancellationToken cancellationToken);
}
