namespace ProcureLive.Contracts.Finance;

public sealed record CurrencyDto(Guid Id, string Code, string Name, string Symbol, bool IsBaseCurrency);

public sealed record CreateCurrencyRequest(string Code, string Name, string Symbol, bool IsBaseCurrency);

public sealed record ExchangeRateDto(Guid Id, Guid CurrencyId, string CurrencyCode, DateOnly RateDate, decimal RateToBase, string? Source);

public sealed record CreateExchangeRateRequest(Guid CurrencyId, DateOnly RateDate, decimal RateToBase, string? Source);
