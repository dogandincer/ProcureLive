using MediatR;
using ProcureLive.Contracts.Finance;

namespace ProcureLive.Application.Finance.Queries.GetExchangeRates;

public sealed record GetExchangeRatesQuery(Guid? CurrencyId, DateOnly? Date) : IRequest<IReadOnlyCollection<ExchangeRateDto>>;
