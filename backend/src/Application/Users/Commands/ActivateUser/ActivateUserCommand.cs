using MediatR;

namespace ProcureLive.Application.Users.Commands.ActivateUser;

public sealed record ActivateUserCommand(Guid UserId) : IRequest;
