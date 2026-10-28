using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfq.Commands.AwardRfqProcess;

[RequiresPermission(PermissionCodes.RfqAward)]
public sealed record AwardRfqProcessCommand(Guid RfqProcessId, Guid WinningQuoteId, string? Notes) : IRequest, IRfqScopedRequest;
