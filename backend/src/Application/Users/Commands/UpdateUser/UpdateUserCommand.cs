using MediatR;

namespace ProcureLive.Application.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(Guid UserId, string FirstName, string LastName, Guid? CompanyId) : IRequest;
