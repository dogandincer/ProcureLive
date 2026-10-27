using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfi.Commands.CloseRfiProcess;

public sealed record CloseRfiProcessCommand(Guid RfiProcessId) : IRequest, IRfiScopedRequest;
