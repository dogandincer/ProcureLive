using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Users;

namespace ProcureLive.Application.Users.Queries.GetUsersList;

public sealed class GetUsersListQueryHandler : IRequestHandler<GetUsersListQuery, PagedResult<UserDto>>
{
    private readonly IIdentityService _identityService;

    public GetUsersListQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersListQuery request, CancellationToken cancellationToken)
    {
        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        PagedResult<ApplicationUserInfo> result = await _identityService.ListUsersAsync(
            request.SearchTerm,
            request.CompanyId,
            page,
            pageSize,
            cancellationToken);

        List<UserDto> items = result.Items
            .Select(u => new UserDto(u.Id, u.Email, u.FirstName, u.LastName, u.Roles, u.CompanyId, u.IsActive, u.LastLoginAt))
            .ToList();

        return new PagedResult<UserDto>(items, result.TotalCount, result.Page, result.PageSize);
    }
}
