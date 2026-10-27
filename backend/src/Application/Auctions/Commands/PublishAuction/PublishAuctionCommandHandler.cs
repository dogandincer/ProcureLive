using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.PublishAuction;

/// <summary>Draft -> Scheduled. Requires at least one item and one invitation, and a StartAt still in the future.</summary>
public sealed class PublishAuctionCommandHandler : IRequestHandler<PublishAuctionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly ICurrentUserService _currentUserService;

    public PublishAuctionCommandHandler(IApplicationDbContext context, IDateTime dateTime, ICurrentUserService currentUserService)
    {
        _context = context;
        _dateTime = dateTime;
        _currentUserService = currentUserService;
    }

    public async Task Handle(PublishAuctionCommand request, CancellationToken cancellationToken)
    {
        Auction? auction = await _context.Auctions
            .Include(a => a.Items)
            .Include(a => a.Invitations)
            .FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        if (auction.Status != AuctionStatus.Draft)
        {
            throw new ConflictException("Yalnızca taslak (Draft) durumundaki ihaleler yayınlanabilir.");
        }

        if (auction.Items.Count == 0)
        {
            throw new ConflictException("İhale yayınlanmadan önce en az bir kalem eklenmelidir.");
        }

        if (auction.Invitations.Count == 0)
        {
            throw new ConflictException("İhale yayınlanmadan önce en az bir Cari davet edilmelidir.");
        }

        DateTime now = _dateTime.UtcNow;

        if (auction.StartAt <= now)
        {
            throw new ConflictException("Başlangıç tarihi gelecekte olmalıdır.");
        }

        AuctionStatusMachine.Apply(_context, auction, AuctionStatus.Scheduled, now, _currentUserService.UserId, null, automatic: false);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
