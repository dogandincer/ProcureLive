using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Auth;

namespace ProcureLive.Application.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        ApplicationUserInfo? user = await _identityService.FindByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("ApplicationUser", request.UserId);
        }

        return new CurrentUserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Roles, user.CompanyId, user.EmailNotificationsEnabled);
    }
}
