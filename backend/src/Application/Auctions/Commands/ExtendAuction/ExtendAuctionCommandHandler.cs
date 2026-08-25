using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.ExtendAuction;

public sealed class ExtendAuctionCommandHandler : IRequestHandler<ExtendAuctionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IAuctionNotificationService _notificationService;

    public ExtendAuctionCommandHandler(
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

    public async Task Handle(ExtendAuctionCommand request, CancellationToken cancellationToken)
    {
        Auction? auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        if (auction.Status != AuctionStatus.Live)
        {
            throw new ConflictException("Yalnızca canlı (Live) durumdaki ihalelerin süresi uzatılabilir.");
        }

        if (request.NewEndAt <= auction.EndAt)
        {
            throw new ConflictException("Yeni bitiş tarihi mevcut bitiş tarihinden sonra olmalıdır.");
        }

        DateTime originalEndAt = auction.EndAt;
        auction.EndAt = request.NewEndAt;

        _context.AuctionExtensionLogs.Add(new AuctionExtensionLog
        {
            AuctionId = auction.Id,
            TriggeredByBidId = null,
            OriginalEndAt = originalEndAt,
            NewEndAt = request.NewEndAt,
            Reason = request.Reason,
            ExtendedByUserId = _currentUserService.UserId,
            CreatedAt = _dateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.AuctionExtendedAsync(auction.Id, request.NewEndAt, request.Reason, cancellationToken);
    }
}
