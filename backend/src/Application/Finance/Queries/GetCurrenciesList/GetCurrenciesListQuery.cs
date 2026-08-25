using MediatR;
using ProcureLive.Contracts.Finance;

namespace ProcureLive.Application.Finance.Queries.GetCurrenciesList;

public sealed record GetCurrenciesListQuery : IRequest<IReadOnlyCollection<CurrencyDto>>;
