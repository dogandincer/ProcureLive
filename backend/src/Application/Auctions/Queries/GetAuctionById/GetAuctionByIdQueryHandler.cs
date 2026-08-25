using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Queries.GetAuctionById;

public sealed class GetAuctionByIdQueryHandler : IRequestHandler<GetAuctionByIdQuery, AuctionDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetAuctionByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuctionDetailDto> Handle(GetAuctionByIdQuery request, CancellationToken cancellationToken)
    {
        Auction? auction = await _context.Auctions
            .Include(a => a.OwnerCompany)
            .Include(a => a.Currency)
            .Include(a => a.Items)
            .Include(a => a.Invitations).ThenInclude(i => i.Company)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.Id);
        }

        var itemDtos = new List<AuctionItemDto>();

        foreach (AuctionItem item in auction.Items.OrderBy(i => i.SortOrder))
        {
            List<Bid> bids = await _context.Bids
                .Where(b => b.AuctionItemId == item.Id)
                .ToListAsync(cancellationToken);

            Bid? best = bids.FirstOrDefault(b => b.Status == BidStatus.Winning);

            itemDtos.Add(new AuctionItemDto(
                item.Id,
                item.ProductId,
                item.Description,
                item.Quantity,
                item.UnitOfMeasure.ToString(),
                item.TargetPrice,
                item.SortOrder,
                best?.Amount.Amount,
                bids.Count));
        }

        List<AuctionInvitationDto> invitationDtos = auction.Invitations
            .Select(i => new AuctionInvitationDto(
                i.Id, i.CompanyId, i.Company.Name, i.Status.ToString(), i.InvitedAt, i.RespondedAt))
            .ToList();

        return new AuctionDetailDto(
            auction.Id,
            auction.Title,
            auction.Description,
            auction.AuctionType.ToString(),
            auction.Status.ToString(),
            auction.OwnerCompanyId,
            auction.OwnerCompany.Name,
            auction.CurrencyId,
            auction.Currency.Code,
            auction.StartAt,
            auction.EndAt,
            auction.ActualStartAt,
            auction.ActualEndAt,
            auction.ReservePrice,
            auction.MinDecrementAmount,
            auction.MinDecrementPercentage,
            auction.ExtensionRuleMinutes,
            itemDtos,
            invitationDtos);
    }
}
