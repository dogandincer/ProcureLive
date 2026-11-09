using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Auth.Commands;

/// <summary>Oturumdaki kullanıcının kişisel tercihleri (şimdilik: bildirimleri e-postayla da alma).</summary>
public sealed record UpdateMyPreferencesCommand(bool EmailNotificationsEnabled) : IRequest;

public sealed class UpdateMyPreferencesCommandHandler : IRequestHandler<UpdateMyPreferencesCommand>
{
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _currentUser;

    public UpdateMyPreferencesCommandHandler(IIdentityService identity, ICurrentUserService currentUser)
    {
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateMyPreferencesCommand request, CancellationToken cancellationToken)
    {
        Guid userId = _currentUser.UserId ?? throw new ForbiddenAccessException("Oturum bulunamadı.");
        if (!await _identity.SetEmailNotificationsAsync(userId, request.EmailNotificationsEnabled, cancellationToken))
        {
            throw new NotFoundException("ApplicationUser", userId);
        }
    }
}
