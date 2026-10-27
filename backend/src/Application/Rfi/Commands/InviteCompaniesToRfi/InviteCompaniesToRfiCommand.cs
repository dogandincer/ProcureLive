using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfi.Commands.InviteCompaniesToRfi;

public sealed record InviteCompaniesToRfiCommand(Guid RfiProcessId, IReadOnlyCollection<Guid> CompanyIds) : IRequest, IRfiScopedRequest;
