using FluentValidation;
using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Auth;
using ProcureLive.Domain.Entities.Auctions;
using RefreshTokenEntity = ProcureLive.Domain.Entities.Identity.RefreshToken;

namespace ProcureLive.Application.Invitations.Commands;

/// <summary>
/// Davet linkiyle anonim kayıt: hesap token'daki cariye Supplier olarak bağlanır, davet garanti edilir,
/// oturum açılmış olarak döner. Token kayıt için tek kullanımlıktır.
/// </summary>
public sealed record RegisterWithInviteCommand(string Token, string Email, string Password, string FirstName, string LastName)
    : IRequest<AuthResultDto>;

public sealed class RegisterWithInviteCommandValidator : AbstractValidator<RegisterWithInviteCommand>
{
    public RegisterWithInviteCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.").MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterWithInviteCommandHandler : IRequestHandler<RegisterWithInviteCommand, AuthResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IDateTime _dateTime;

    public RegisterWithInviteCommandHandler(
        IApplicationDbContext context, IIdentityService identityService, ITokenService tokenService, IDateTime dateTime)
    {
        _context = context;
        _identityService = identityService;
        _tokenService = tokenService;
        _dateTime = dateTime;
    }

    public async Task<AuthResultDto> Handle(RegisterWithInviteCommand request, CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;
        InvitationToken token = await InviteRules.LoadUsableAsync(_context, request.Token, now, cancellationToken);

        if (token.UsedAt is not null)
            throw new ConflictException("Bu davet linkiyle zaten hesap oluşturulmuş. Lütfen giriş yapın.", ErrorCodes.InviteUsed);

        string email = request.Email.Trim().ToLowerInvariant();
        if (token.Email is not null && !string.Equals(token.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Bu davet farklı bir e-posta adresi için oluşturulmuş.", ErrorCodes.InviteEmailMismatch);

        if (await _identityService.FindByEmailAsync(email, cancellationToken) is not null)
            throw new ConflictException("Bu e-posta ile zaten bir hesap var. Lütfen giriş yapın.", ErrorCodes.AccountExists);

        var createResult = await _identityService.CreateUserAsync(
            email, request.Password, request.FirstName.Trim(), request.LastName.Trim(), Roles.Supplier, token.CompanyId, cancellationToken);

        if (!createResult.Succeeded || createResult.UserId is null)
            throw new ConflictException(string.Join("; ", createResult.Errors));

        ApplicationUserInfo user = await _identityService.FindByIdAsync(createResult.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("ApplicationUser", createResult.UserId.Value);

        token.UsedAt = now;
        token.RegisteredUserId = user.Id;
        await InviteRules.EnsureInvitedAsync(_context, token.AuctionId, token.CompanyId, token.CreatedByUserId, now, cancellationToken);

        (string accessToken, DateTime expiresAt) = _tokenService.GenerateAccessToken(user);
        string refreshTokenValue = _tokenService.GenerateRefreshToken();
        _context.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = now.AddDays(7),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            user.Id, user.Email, user.FirstName, user.LastName, user.Roles, user.CompanyId, accessToken, expiresAt, refreshTokenValue);
    }
}
