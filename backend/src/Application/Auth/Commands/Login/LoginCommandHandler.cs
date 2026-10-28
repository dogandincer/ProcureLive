using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Auth;
using RefreshTokenEntity = ProcureLive.Domain.Entities.Identity.RefreshToken;

namespace ProcureLive.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    /// <summary>"Şifresini 3'ten fazla [hatalı] giren kullanıcı pasife alınır" → 4. hatalı denemede.</summary>
    public const int MaxFailedAttempts = 3;

    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public LoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IApplicationDbContext context,
        IDateTime dateTime)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        ApplicationUserInfo? user = await _identityService.FindByEmailAsync(request.Email, cancellationToken);

        // Genel mesaj hesabın varlığını ele vermez; kilit kuralını baştan söyler.
        const string invalidCredentials = "E-posta veya şifre hatalı. Art arda 3'ten fazla hatalı denemede hesap kilitlenir.";

        if (user is null)
        {
            throw new ForbiddenAccessException(invalidCredentials, ErrorCodes.InvalidCredentials);
        }

        bool passwordValid = await _identityService.CheckPasswordAsync(user.Id, request.Password, cancellationToken);

        if (!passwordValid)
        {
            // Art arda 3'ten fazla hatalı şifre → hesap pasife alınır. Kilit mesajı yalnızca doğru şifreden
            // sonra gösterilir; yanlış denemeler hesabın varlığını/durumunu ele vermez.
            if (user.IsActive)
            {
                await _identityService.RegisterFailedLoginAsync(user.Id, _dateTime.UtcNow, MaxFailedAttempts, cancellationToken);
            }

            throw new ForbiddenAccessException(invalidCredentials, ErrorCodes.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            if (user.LockedOutAt is not null)
            {
                throw new ForbiddenAccessException(
                    "Hesabınız art arda 3'ten fazla hatalı şifre girildiği için pasife alındı. Yöneticinize başvurun.",
                    ErrorCodes.AccountLocked);
            }

            // Şifre doğru ama hesap yönetici tarafından pasife alınmış: kullanıcı ne yapacağını bilsin.
            throw new ForbiddenAccessException("Hesabınız pasif durumda. Yeniden açılması için yöneticinize başvurun.", ErrorCodes.AccountInactive);
        }

        await _identityService.ResetFailedLoginsAsync(user.Id, cancellationToken);

        (string accessToken, DateTime expiresAt) = _tokenService.GenerateAccessToken(user);
        string refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _dateTime.UtcNow.AddDays(7),
            CreatedAt = _dateTime.UtcNow,
            CreatedByIp = request.IpAddress
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
        await _identityService.UpdateLastLoginAsync(user.Id, _dateTime.UtcNow, cancellationToken);

        return new AuthResultDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles,
            user.CompanyId,
            accessToken,
            expiresAt,
            refreshTokenValue);
    }
}
