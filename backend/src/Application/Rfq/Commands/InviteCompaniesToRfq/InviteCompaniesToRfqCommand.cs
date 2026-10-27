using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfq.Commands.InviteCompaniesToRfq;

public sealed record InviteCompaniesToRfqCommand(Guid RfqProcessId, IReadOnlyCollection<Guid> CompanyIds) : IRequest, IRfqScopedRequest;
