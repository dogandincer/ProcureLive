using MediatR;

namespace ProcureLive.Application.Finance.Commands.CreateExchangeRate;

public sealed record CreateExchangeRateCommand(
    Guid CurrencyId,
    DateOnly RateDate,
    decimal RateToBase,
    string? Source) : IRequest<Guid>;
