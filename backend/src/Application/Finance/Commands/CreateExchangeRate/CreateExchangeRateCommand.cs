using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Finance.Commands.CreateExchangeRate;

[RequiresPermission(PermissionCodes.CatalogManage)]
public sealed record CreateExchangeRateCommand(
    Guid CurrencyId,
    DateOnly RateDate,
    decimal RateToBase,
    string? Source) : IRequest<Guid>;
