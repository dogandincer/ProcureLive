using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfq.Commands.PublishRfqProcess;

/// <summary>RFQ setup step 4/4: review and publish (moves Sent -> InProgress, open for quotes).</summary>
[RequiresPermission(PermissionCodes.RfqManage)]
public sealed record PublishRfqProcessCommand(Guid RfqProcessId) : IRequest, IRfqScopedRequest;
