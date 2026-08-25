using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IIdentityService _identityService;

    public UpdateUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        bool updated = await _identityService.UpdateProfileAsync(
            request.UserId,
            request.FirstName,
            request.LastName,
            request.CompanyId,
            cancellationToken);

        if (!updated)
        {
            throw new NotFoundException("ApplicationUser", request.UserId);
        }
    }
}
