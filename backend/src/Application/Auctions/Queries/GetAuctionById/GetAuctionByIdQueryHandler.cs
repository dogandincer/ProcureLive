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
            .Include(a => a.Attachments)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.Id);
        }

        // Tüm kalemlerin teklifleri tek sorguda (kalem başına sorgu/N+1 yerine), bellekte gruplanır.
        List<Guid> itemIds = auction.Items.Select(i => i.Id).ToList();
        Dictionary<Guid, List<Bid>> bidsByItem = (await _context.Bids
                .Where(b => itemIds.Contains(b.AuctionItemId))
                .ToListAsync(cancellationToken))
            .GroupBy(b => b.AuctionItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<AuctionItemDto> itemDtos = auction.Items
            .OrderBy(i => i.SortOrder)
            .Select(item =>
            {
                List<Bid> bids = bidsByItem.TryGetValue(item.Id, out List<Bid>? b) ? b : new List<Bid>();
                Bid? best = bids.FirstOrDefault(x => x.Status == BidStatus.Winning);
                return new AuctionItemDto(
                    item.Id,
                    item.ProductId,
                    item.Description,
                    item.Quantity,
                    item.UnitOfMeasure.ToString(),
                    item.TargetPrice,
                    item.RequestedDeliveryDate,
                    item.Specification,
                    item.SortOrder,
                    best?.Amount.Amount,
                    bids.Count);
            })
            .ToList();

        List<AuctionInvitationDto> invitationDtos = auction.Invitations
            .Select(i => new AuctionInvitationDto(
                i.Id, i.CompanyId, i.Company.Name, i.Status.ToString(), i.InvitedAt, i.RespondedAt))
            .ToList();

        List<AuctionAttachmentDto> attachmentDtos = auction.Attachments
            .Select(a => new AuctionAttachmentDto(a.Id, a.FileName, a.Url, a.ContentType, a.SizeBytes))
            .ToList();

        return new AuctionDetailDto(
            auction.Id,
            auction.Title,
            auction.ReferenceCode,
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
            auction.ExtensionDurationMinutes,
            auction.MaxExtensionMinutes,
            auction.PaymentTerm?.ToString(),
            auction.DeliveryTerm?.ToString(),
            auction.AllowAlternativeProducts,
            auction.AllowPartialBids,
            itemDtos,
            invitationDtos,
            attachmentDtos);
    }
}
