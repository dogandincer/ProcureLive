using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;

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
        bool succeeded = await _identityService.SetRolesAsync(request.UserId, request.Roles, cancellationToken);

        if (!succeeded)
        {
            throw new NotFoundException("ApplicationUser", request.UserId);
        }
    }
}
