using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfq.Commands.AwardRfqProcess;

public sealed record AwardRfqProcessCommand(Guid RfqProcessId, Guid WinningQuoteId, string? Notes) : IRequest, IRfqScopedRequest;
