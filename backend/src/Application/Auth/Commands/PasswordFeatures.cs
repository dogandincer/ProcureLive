using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;

namespace ProcureLive.Application.Auth.Commands;

// ---------------------------------------------------------------- Şifremi unuttum (anonim)

/// <summary>
/// Sıfırlama bağlantısını e-postayla gönderir. Hesabın varlığı ele verilmesin diye her durumda aynı yanıt döner;
/// yönetici tarafından pasife alınmış hesaba bağlantı gönderilmez (kilitli hesap sıfırlamayla yeniden açılabilir).
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : IRequest;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");
    }
}

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IIdentityService _identity;
    private readonly IEmailSender _email;
    private readonly IAppUrlProvider _urls;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(IIdentityService identity, IEmailSender email, IAppUrlProvider urls, ILogger<ForgotPasswordCommandHandler> logger)
    {
        _identity = identity;
        _email = email;
        _urls = urls;
        _logger = logger;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        ApplicationUserInfo? user = await _identity.FindByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || (!user.IsActive && user.LockedOutAt is null)) return;

        string? token = await _identity.GeneratePasswordResetTokenAsync(user.Id, cancellationToken);
        if (token is null) return;

        string link = $"{_urls.FrontendBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
        try
        {
            await _email.SendAsync(new EmailMessage(user.Email, "Şifre sıfırlama",
                $"Merhaba {user.FirstName},\n\nŞifrenizi yenilemek için aşağıdaki düğmeyi kullanın. Bağlantı 24 saat geçerlidir.\n\n" +
                "Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.",
                link, "Şifremi yenile"), cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Yanıt her durumda aynı kalmalı (hesap varlığı ele verilmesin); SMTP hatası yalnızca sunucu loguna yazılır.
            _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi: {UserId}", user.Id);
        }
    }
}

// ---------------------------------------------------------------- Sıfırlama bağlantısıyla yeni şifre (anonim)

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IIdentityService _identity;

    public ResetPasswordCommandHandler(IIdentityService identity) => _identity = identity;

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        const string invalid = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş. Yeni bir bağlantı isteyin.";
        ApplicationUserInfo user = await _identity.FindByEmailAsync(request.Email.Trim(), cancellationToken)
            ?? throw new ConflictException(invalid);

        IdentityOperationResult result = await _identity.ResetPasswordAsync(user.Id, request.Token, request.NewPassword, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ConflictException(result.Errors.Contains("InvalidToken") ? invalid : string.Join(" ", result.Errors));
        }
    }
}

// ---------------------------------------------------------------- Oturum açmış kullanıcı şifresini değiştirir

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Mevcut şifrenizi girin.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
        RuleFor(x => x.NewPassword).NotEqual(x => x.CurrentPassword).WithMessage("Yeni şifre mevcut şifreden farklı olmalıdır.");
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordCommandHandler(IIdentityService identity, ICurrentUserService currentUser)
    {
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        Guid userId = _currentUser.UserId ?? throw new ForbiddenAccessException("Oturum bulunamadı.");
        IdentityOperationResult result = await _identity.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ConflictException(result.Errors.Contains("PasswordMismatch") ? "Mevcut şifre hatalı." : string.Join(" ", result.Errors));
        }
    }
}
