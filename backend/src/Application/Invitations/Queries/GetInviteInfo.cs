using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Invitations;

namespace ProcureLive.Application.Invitations.Queries;

/// <summary>Anonim karşılama ekranı verisi. Geçersiz token'da hata fırlatmaz; IsValid=false + kod döner.</summary>
public sealed record GetInviteInfoQuery(string Token) : IRequest<InviteInfoDto>;

public sealed class GetInviteInfoQueryHandler : IRequestHandler<GetInviteInfoQuery, InviteInfoDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public GetInviteInfoQueryHandler(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<InviteInfoDto> Handle(GetInviteInfoQuery request, CancellationToken cancellationToken)
    {
        var t = await _context.InvitationTokens
            .Where(x => x.Token == request.Token)
            .Select(x => new
            {
                x.AuctionId, x.CompanyId, x.Email, x.ExpiresAt, x.UsedAt,
                AuctionTitle = x.Auction.Title, x.Auction.ReferenceCode, AuctionStatus = x.Auction.Status, AuctionEndAt = x.Auction.EndAt,
                OwnerName = x.Auction.OwnerCompany.Name, CompanyName = x.Company.Name,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (t is null)
        {
            return new InviteInfoDto(false, ErrorCodes.InviteInvalid, null, null, null, null, null, null, null, null, null, false);
        }

        string? reason = t.ExpiresAt <= _dateTime.UtcNow ? ErrorCodes.InviteExpired
            : InviteRules.IsAuctionClosed(t.AuctionStatus) ? ErrorCodes.AuctionClosed
            : null;

        return new InviteInfoDto(
            reason is null, reason, t.AuctionId, t.AuctionTitle, t.ReferenceCode, t.OwnerName,
            t.CompanyId, t.CompanyName, t.Email, t.ExpiresAt, t.AuctionEndAt, t.UsedAt is not null);
    }
}
