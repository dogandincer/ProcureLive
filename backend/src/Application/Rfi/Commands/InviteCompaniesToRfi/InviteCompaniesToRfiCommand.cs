using MediatR;

namespace ProcureLive.Application.Rfi.Commands.InviteCompaniesToRfi;

public sealed record InviteCompaniesToRfiCommand(Guid RfiProcessId, IReadOnlyCollection<Guid> CompanyIds) : IRequest;
