using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.AwardAuctionItem;

public sealed class AwardAuctionItemCommandHandler : IRequestHandler<AwardAuctionItemCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IAuctionNotificationService _notificationService;

    public AwardAuctionItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IAuctionNotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _notificationService = notificationService;
    }

    public async Task Handle(AwardAuctionItemCommand request, CancellationToken cancellationToken)
    {
        Auction? auction = await _context.Auctions
            .Include(a => a.Items)
            .FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        if (auction.Status is not (AuctionStatus.Ended or AuctionStatus.Live))
        {
            throw new ConflictException("Yalnızca sona ermiş (veya canlı) ihalelerde kazanan seçilebilir.");
        }

        AuctionItem? item = auction.Items.FirstOrDefault(i => i.Id == request.AuctionItemId);

        if (item is null)
        {
            throw new NotFoundException(nameof(AuctionItem), request.AuctionItemId);
        }

        bool alreadyAwarded = await _context.AuctionAwards.AnyAsync(
            a => a.AuctionItemId == request.AuctionItemId, cancellationToken);

        if (alreadyAwarded)
        {
            throw new ConflictException("Bu kalem için zaten bir kazanan seçilmiş.");
        }

        Bid? winningBid = await _context.Bids.FirstOrDefaultAsync(
            b => b.Id == request.WinningBidId && b.AuctionItemId == request.AuctionItemId, cancellationToken);

        if (winningBid is null)
        {
            throw new NotFoundException(nameof(Bid), request.WinningBidId);
        }

        DateTime now = _dateTime.UtcNow;

        _context.AuctionAwards.Add(new AuctionAward
        {
            AuctionId = auction.Id,
            AuctionItemId = item.Id,
            WinningBidId = winningBid.Id,
            AwardedCompanyId = winningBid.CompanyId,
            AwardedByUserId = _currentUserService.UserId ?? Guid.Empty,
            AwardedAt = now,
            Notes = request.Notes
        });

        List<AuctionInvitation> invitations = await _context.AuctionInvitations
            .Where(i => i.AuctionId == auction.Id)
            .ToListAsync(cancellationToken);

        foreach (AuctionInvitation invitation in invitations)
        {
            invitation.Status = invitation.CompanyId == winningBid.CompanyId
                ? InvitationStatus.Won
                : InvitationStatus.Lost;
        }

        await _context.SaveChangesAsync(cancellationToken);

        int awardedItemCount = await _context.AuctionAwards
            .Where(a => a.AuctionId == auction.Id)
            .Select(a => a.AuctionItemId)
            .Distinct()
            .CountAsync(cancellationToken);

        if (awardedItemCount >= auction.Items.Count)
        {
            auction.Status = AuctionStatus.Awarded;
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _notificationService.ItemAwardedAsync(auction.Id, item.Id, winningBid.CompanyId, cancellationToken);
    }
}
