using MediatR;

namespace ProcureLive.Application.Rfq.Commands.InviteCompaniesToRfq;

public sealed record InviteCompaniesToRfqCommand(Guid RfqProcessId, IReadOnlyCollection<Guid> CompanyIds) : IRequest;
