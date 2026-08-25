using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.AddAuctionItem;

public sealed class AddAuctionItemCommandHandler : IRequestHandler<AddAuctionItemCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddAuctionItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddAuctionItemCommand request, CancellationToken cancellationToken)
    {
        Auction? auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        if (auction.Status != AuctionStatus.Draft)
        {
            throw new ConflictException("Sadece taslak (Draft) durumundaki ihalelere kalem eklenebilir.");
        }

        var item = new AuctionItem
        {
            AuctionId = request.AuctionId,
            ProductId = request.ProductId,
            Description = request.Description,
            Quantity = request.Quantity,
            UnitOfMeasure = request.UnitOfMeasure,
            TargetPrice = request.TargetPrice,
            SortOrder = request.SortOrder
        };

        _context.AuctionItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
