using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfi.Commands.CloseRfiProcess;

[RequiresPermission(PermissionCodes.RfiManage)]
public sealed record CloseRfiProcessCommand(Guid RfiProcessId) : IRequest, IRfiScopedRequest;
