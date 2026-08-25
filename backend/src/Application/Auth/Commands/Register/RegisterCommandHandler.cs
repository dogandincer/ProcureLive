using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Auth;
using RefreshTokenEntity = ProcureLive.Domain.Entities.Identity.RefreshToken;

namespace ProcureLive.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public RegisterCommandHandler(
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

    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var createResult = await _identityService.CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Role,
            request.CompanyId,
            cancellationToken);

        if (!createResult.Succeeded || createResult.UserId is null)
        {
            throw new ConflictException(string.Join("; ", createResult.Errors));
        }

        ApplicationUserInfo? user = await _identityService.FindByIdAsync(createResult.UserId.Value, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("ApplicationUser", createResult.UserId.Value);
        }

        (string accessToken, DateTime expiresAt) = _tokenService.GenerateAccessToken(user);
        string refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _dateTime.UtcNow.AddDays(7),
            CreatedAt = _dateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

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
