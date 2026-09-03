using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Users.Commands.ActivateUser;

public sealed class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand>
{
    private readonly IIdentityService _identityService;

    public ActivateUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        bool succeeded = await _identityService.SetActiveAsync(request.UserId, true, cancellationToken);

        if (!succeeded)
        {
            throw new NotFoundException("ApplicationUser", request.UserId);
        }
    }
}
