using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Auth;
using RefreshTokenEntity = ProcureLive.Domain.Entities.Identity.RefreshToken;

namespace ProcureLive.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IDateTime _dateTime;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        ITokenService tokenService,
        IDateTime dateTime)
    {
        _context = context;
        _identityService = identityService;
        _tokenService = tokenService;
        _dateTime = dateTime;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        RefreshTokenEntity? existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            throw new ForbiddenAccessException("Geçersiz veya süresi dolmuş refresh token.");
        }

        ApplicationUserInfo? user = await _identityService.FindByIdAsync(existingToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new ForbiddenAccessException("Kullanıcı bulunamadı veya pasif.");
        }

        string newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        existingToken.RevokedAt = _dateTime.UtcNow;
        existingToken.ReplacedByToken = newRefreshTokenValue;

        var newRefreshToken = new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = _dateTime.UtcNow.AddDays(7),
            CreatedAt = _dateTime.UtcNow,
            CreatedByIp = request.IpAddress
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        (string accessToken, DateTime expiresAt) = _tokenService.GenerateAccessToken(user);

        return new AuthResultDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles,
            user.CompanyId,
            accessToken,
            expiresAt,
            newRefreshTokenValue);
    }
}
