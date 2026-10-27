using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.AwardAuctionItem;

public sealed class AwardAuctionItemCommandHandler : IRequestHandler<AwardAuctionItemCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IAuctionNotificationService _notificationService;
    private readonly INotificationRealtimeService _notificationRealtimeService;

    public AwardAuctionItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IAuctionNotificationService notificationService,
        INotificationRealtimeService notificationRealtimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _notificationService = notificationService;
        _notificationRealtimeService = notificationRealtimeService;
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
            // Son kalemin kazananı seçilince sistem sonuçlandırır (geçmişte otomatik + işlemi yapan kullanıcı).
            AuctionStatusMachine.Apply(_context, auction, AuctionStatus.Awarded, now, _currentUserService.UserId, null, automatic: true);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _notificationService.ItemAwardedAsync(auction.Id, item.Id, winningBid.CompanyId, cancellationToken);

        // SignalR (above) only reaches a company actively watching this auction's live page - the
        // persisted Notification + queued email below is what actually informs a winner who isn't
        // online at award time. Mirrors AwardRfqProcessCommandHandler's notification pattern.
        await NotificationCreator.CreateForCompanyAsync(
            _context, _notificationRealtimeService, _dateTime, winningBid.CompanyId,
            NotificationType.AwardNotice,
            "İhaleyi Kazandınız",
            $"\"{auction.Title}\" ihalesinde \"{item.Description}\" kalemi için teklifiniz kazandı.",
            nameof(Auction), auction.Id, cancellationToken);

        List<Guid> losingCompanyIds = await _context.Bids
            .Where(b => b.AuctionItemId == item.Id && b.CompanyId != winningBid.CompanyId)
            .Select(b => b.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (Guid companyId in losingCompanyIds)
        {
            await NotificationCreator.CreateForCompanyAsync(
                _context, _notificationRealtimeService, _dateTime, companyId,
                NotificationType.AwardNotice,
                "İhale Sonuçlandı",
                $"\"{auction.Title}\" ihalesinde \"{item.Description}\" kalemi için başka bir tedarikçinin teklifi seçildi.",
                nameof(Auction), auction.Id, cancellationToken);
        }
    }
}
