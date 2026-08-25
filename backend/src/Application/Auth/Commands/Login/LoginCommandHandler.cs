using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Auth;
using RefreshTokenEntity = ProcureLive.Domain.Entities.Identity.RefreshToken;

namespace ProcureLive.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
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

        if (user is null || !user.IsActive)
        {
            throw new ForbiddenAccessException("E-posta veya şifre hatalı.");
        }

        bool passwordValid = await _identityService.CheckPasswordAsync(user.Id, request.Password, cancellationToken);

        if (!passwordValid)
        {
            throw new ForbiddenAccessException("E-posta veya şifre hatalı.");
        }

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
