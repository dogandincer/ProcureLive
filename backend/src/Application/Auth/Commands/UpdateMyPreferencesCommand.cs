using FluentValidation;
using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Auth.Commands;

/// <summary>
/// Oturumdaki kullanıcının kişisel tercihleri: bildirimleri e-postayla da alma ve arayüz dili ("tr" / "en").
/// Yalnızca gönderilen alanlar değişir (null = olduğu gibi kalsın).
/// </summary>
public sealed record UpdateMyPreferencesCommand(bool? EmailNotificationsEnabled, string? Language = null) : IRequest;

public sealed class UpdateMyPreferencesCommandValidator : AbstractValidator<UpdateMyPreferencesCommand>
{
    public UpdateMyPreferencesCommandValidator()
    {
        RuleFor(x => x.Language)
            .Must(l => l is null or "tr" or "en")
            .WithMessage("Dil 'tr' ya da 'en' olmalıdır.");
    }
}

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
        if (!await _identity.SetPreferencesAsync(userId, request.EmailNotificationsEnabled, request.Language, cancellationToken))
        {
            throw new NotFoundException("ApplicationUser", userId);
        }
    }
}
