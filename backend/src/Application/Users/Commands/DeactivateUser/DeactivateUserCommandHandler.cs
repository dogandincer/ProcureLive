using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Users.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IIdentityService _identityService;

    public DeactivateUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        bool succeeded = await _identityService.SetActiveAsync(request.UserId, false, cancellationToken);

        if (!succeeded)
        {
            throw new NotFoundException("ApplicationUser", request.UserId);
        }
    }
}
