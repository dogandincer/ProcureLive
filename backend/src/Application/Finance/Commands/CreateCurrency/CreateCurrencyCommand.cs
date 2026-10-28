using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Finance.Commands.CreateCurrency;

[RequiresPermission(PermissionCodes.CatalogManage)]
public sealed record CreateCurrencyCommand(string Code, string Name, string Symbol, bool IsBaseCurrency) : IRequest<Guid>;
