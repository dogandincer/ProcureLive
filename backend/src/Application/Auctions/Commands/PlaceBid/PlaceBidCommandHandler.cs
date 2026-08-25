using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Common;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;
using ProcureLive.Domain.ValueObjects;

namespace ProcureLive.Application.Auctions.Commands.PlaceBid;

public sealed class PlaceBidCommandHandler : IRequestHandler<PlaceBidCommand, BidDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IAuctionCacheService _cacheService;
    private readonly IAuctionNotificationService _notificationService;

    public PlaceBidCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IAuctionCacheService cacheService,
        IAuctionNotificationService notificationService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _cacheService = cacheService;
        _notificationService = notificationService;
    }

    public async Task<BidDto> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        Guid? companyId = _currentUserService.CompanyId;
        Guid userId = _currentUserService.UserId ?? Guid.Empty;

        if (companyId is null)
        {
            throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
        }

        Auction? auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        AuctionInvitation? invitation = await _context.AuctionInvitations.FirstOrDefaultAsync(
            i => i.AuctionId == request.AuctionId && i.CompanyId == companyId.Value,
            cancellationToken);

        bool isInvitedAndAccepted = invitation is not null
            && (invitation.Status == InvitationStatus.Accepted || invitation.Status == InvitationStatus.Participating);

        if (!isInvitedAndAccepted)
        {
            throw new ForbiddenAccessException("Bu ihaleye teklif vermek için davetin kabul edilmiş olması gerekir.");
        }

        DateTime now = _dateTime.UtcNow;

        if (auction.Status != AuctionStatus.Live || now >= auction.EndAt)
        {
            throw new ConflictException("İhale şu anda teklif kabul etmiyor.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        // Row-level lock on the item for the duration of the transaction so two simultaneous bids
        // on the same item can never both believe they are the current best.
        await _context.ExecuteRawSqlAsync(
            "SELECT 1 FROM \"AuctionItems\" WHERE \"Id\" = {0} FOR UPDATE",
            new object[] { request.AuctionItemId },
            cancellationToken);

        AuctionItem? item = await _context.AuctionItems
            .FirstOrDefaultAsync(i => i.Id == request.AuctionItemId && i.AuctionId == request.AuctionId, cancellationToken);

        if (item is null)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw new NotFoundException(nameof(AuctionItem), request.AuctionItemId);
        }

        Bid? currentWinningBid = await _context.Bids
            .FirstOrDefaultAsync(b => b.AuctionItemId == item.Id && b.Status == BidStatus.Winning, cancellationToken);

        decimal normalizedNewAmount = await BidCurrencyNormalizer.NormalizeToBaseCurrencyAsync(
            _context, request.Amount, request.CurrencyId, cancellationToken);

        if (currentWinningBid is not null && auction.AuctionType != AuctionType.SealedBid)
        {
            decimal normalizedCurrentBest = await BidCurrencyNormalizer.NormalizeToBaseCurrencyAsync(
                _context, currentWinningBid.Amount.Amount, currentWinningBid.Amount.CurrencyId, cancellationToken);

            bool beatsCurrentBest = auction.AuctionType == AuctionType.English
                ? normalizedNewAmount > normalizedCurrentBest
                : normalizedNewAmount < normalizedCurrentBest;

            if (!beatsCurrentBest)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                string direction = auction.AuctionType == AuctionType.English ? "yüksek" : "düşük";
                throw new ConflictException($"Teklifiniz mevcut en iyi tekliften daha {direction} olmalıdır.");
            }

            currentWinningBid.Status = BidStatus.Outbid;
        }

        var bid = new Bid
        {
            AuctionItemId = item.Id,
            CompanyId = companyId.Value,
            UserId = userId,
            Amount = new Money(request.Amount, request.CurrencyId),
            BidTime = now,
            Status = BidStatus.Winning,
            IpAddress = _currentUserService.IpAddress
        };

        _context.Bids.Add(bid);

        if (invitation!.Status == InvitationStatus.Accepted)
        {
            invitation.Status = InvitationStatus.Participating;
        }

        // Anti-sniping: a bid landing within ExtensionRuleMinutes of EndAt pushes EndAt out by the same window.
        bool triggersExtension = auction.ExtensionRuleMinutes > 0
            && auction.EndAt - now <= TimeSpan.FromMinutes(auction.ExtensionRuleMinutes);

        DateTime? newEndAt = null;

        if (triggersExtension)
        {
            DateTime originalEndAt = auction.EndAt;
            newEndAt = now.AddMinutes(auction.ExtensionRuleMinutes);
            auction.EndAt = newEndAt.Value;

            _context.AuctionExtensionLogs.Add(new AuctionExtensionLog
            {
                AuctionId = auction.Id,
                TriggeredByBidId = bid.Id,
                OriginalEndAt = originalEndAt,
                NewEndAt = newEndAt.Value,
                Reason = "Anti-sniping: son dakika teklifi nedeniyle otomatik uzatma.",
                ExtendedByUserId = null,
                CreatedAt = now
            });
        }

        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        await _cacheService.SetBestAmountAsync(item.Id, normalizedNewAmount, cancellationToken);

        var bidDto = new BidDto(
            bid.Id,
            bid.AuctionItemId,
            bid.CompanyId,
            string.Empty,
            bid.Amount.Amount,
            bid.Amount.CurrencyId,
            bid.BidTime,
            bid.Status.ToString(),
            IsMine: true);

        await _notificationService.BidPlacedAsync(auction.Id, bidDto, cancellationToken);

        if (newEndAt.HasValue)
        {
            await _notificationService.AuctionExtendedAsync(
                auction.Id, newEndAt.Value, "Anti-sniping: son dakika teklifi nedeniyle otomatik uzatma.", cancellationToken);
        }

        return bidDto;
    }
}
