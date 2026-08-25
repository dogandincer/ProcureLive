using MediatR;

namespace ProcureLive.Application.Finance.Commands.CreateCurrency;

public sealed record CreateCurrencyCommand(string Code, string Name, string Symbol, bool IsBaseCurrency) : IRequest<Guid>;
