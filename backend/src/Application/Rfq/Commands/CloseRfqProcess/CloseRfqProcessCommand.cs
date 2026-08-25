using MediatR;

namespace ProcureLive.Application.Rfq.Commands.CloseRfqProcess;

public sealed record CloseRfqProcessCommand(Guid RfqProcessId) : IRequest;
