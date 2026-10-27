using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfq.Commands.CloseRfqProcess;

public sealed record CloseRfqProcessCommand(Guid RfqProcessId) : IRequest, IRfqScopedRequest;
