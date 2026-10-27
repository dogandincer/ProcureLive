using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Invitations;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Application.Invitations.Commands;

/// <summary>Hesabı olan (aynı cariye bağlı) kullanıcı davet linkini sahiplenir; davet garanti edilir.</summary>
public sealed record ClaimInviteCommand(string Token) : IRequest<ClaimInviteResultDto>;

public sealed class ClaimInviteCommandHandler : IRequestHandler<ClaimInviteCommand, ClaimInviteResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public ClaimInviteCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<ClaimInviteResultDto> Handle(ClaimInviteCommand request, CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;
        InvitationToken token = await InviteRules.LoadUsableAsync(_context, request.Token, now, cancellationToken);

        if (_currentUserService.CompanyId != token.CompanyId)
            throw new ForbiddenAccessException(
                "Bu davet başka bir cariye ait. Davet edilen firmanın hesabıyla giriş yapın.", ErrorCodes.InviteCompanyMismatch);

        await InviteRules.EnsureInvitedAsync(_context, token.AuctionId, token.CompanyId, token.CreatedByUserId, now, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClaimInviteResultDto(token.AuctionId);
    }
}
