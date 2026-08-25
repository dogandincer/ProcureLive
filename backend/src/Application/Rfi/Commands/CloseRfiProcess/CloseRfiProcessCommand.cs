using MediatR;

namespace ProcureLive.Application.Rfi.Commands.CloseRfiProcess;

public sealed record CloseRfiProcessCommand(Guid RfiProcessId) : IRequest;
