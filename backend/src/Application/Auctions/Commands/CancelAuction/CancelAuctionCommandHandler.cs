using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.CancelAuction;

public sealed class CancelAuctionCommandHandler : IRequestHandler<CancelAuctionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuctionNotificationService _notificationService;

    public CancelAuctionCommandHandler(IApplicationDbContext context, IAuctionNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(CancelAuctionCommand request, CancellationToken cancellationToken)
    {
        Auction? auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        if (auction.Status is AuctionStatus.Ended or AuctionStatus.Awarded or AuctionStatus.Cancelled)
        {
            throw new ConflictException("Sona ermiş veya sonuçlanmış bir ihale iptal edilemez.");
        }

        auction.Status = AuctionStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.AuctionEndedAsync(auction.Id, cancellationToken);
    }
}
