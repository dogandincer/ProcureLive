using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfq.Commands.CloseRfqProcess;

[RequiresPermission(PermissionCodes.RfqManage)]
public sealed record CloseRfqProcessCommand(Guid RfqProcessId) : IRequest, IRfqScopedRequest;
