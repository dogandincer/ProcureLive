using MediatR;

namespace ProcureLive.Application.Rfq.Commands.AwardRfqProcess;

public sealed record AwardRfqProcessCommand(Guid RfqProcessId, Guid WinningQuoteId, string? Notes) : IRequest;
