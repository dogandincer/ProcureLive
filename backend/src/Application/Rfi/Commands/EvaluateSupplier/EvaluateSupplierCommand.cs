using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfi.Commands.EvaluateSupplier;

[RequiresPermission(PermissionCodes.RfiManage)]
public sealed record EvaluateSupplierCommand(Guid RfiProcessId, Guid CompanyId, decimal TotalScore, string? Comments) : IRequest<Guid>, IRfiScopedRequest;
