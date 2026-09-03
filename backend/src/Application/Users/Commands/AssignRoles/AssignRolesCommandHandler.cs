using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Users.Commands.AssignRoles;

public sealed class AssignRolesCommandHandler : IRequestHandler<AssignRolesCommand>
{
    private readonly IIdentityService _identityService;

    public AssignRolesCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(AssignRolesCommand request, CancellationToken cancellationToken)
    {
        if (request.Roles.Contains(Roles.Supplier))
        {
            ApplicationUserInfo? user = await _identityService.FindByIdAsync(request.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("ApplicationUser", request.UserId);
            }

            if (user.CompanyId is null)
            {
                throw new ConflictException(
                    "Supplier rolü atamak için önce kullanıcıya bir Cari atanmalıdır (Kullanıcı bilgilerini güncelleyin).");
            }
        }

        bool succeeded = await _identityService.SetRolesAsync(request.UserId, request.Roles, cancellationToken);

        if (!succeeded)
        {
            throw new NotFoundException("ApplicationUser", request.UserId);
        }
    }
}
