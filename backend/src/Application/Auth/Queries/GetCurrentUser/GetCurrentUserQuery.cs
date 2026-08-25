using MediatR;
using ProcureLive.Contracts.Auth;

namespace ProcureLive.Application.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserDto>;
