using MediatR;

namespace ProcureLive.Application.Users.Commands.AssignRoles;

public sealed record AssignRolesCommand(Guid UserId, IReadOnlyCollection<string> Roles) : IRequest;
