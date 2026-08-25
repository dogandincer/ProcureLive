using MediatR;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Users;

namespace ProcureLive.Application.Users.Queries.GetUsersList;

public sealed record GetUsersListQuery(string? SearchTerm, Guid? CompanyId, int Page, int PageSize)
    : IRequest<PagedResult<UserDto>>;
