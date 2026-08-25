using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Domain.Entities.Auctions;

public class AuctionAward : AuditableEntity
{
    public Guid AuctionId { get; set; }

    public Auction Auction { get; set; } = null!;

    public Guid AuctionItemId { get; set; }

    public AuctionItem AuctionItem { get; set; } = null!;

    public Guid WinningBidId { get; set; }

    public Bid WinningBid { get; set; } = null!;

    public Guid AwardedCompanyId { get; set; }

    public Company AwardedCompany { get; set; } = null!;

    public Guid AwardedByUserId { get; set; }

    public DateTime AwardedAt { get; set; }

    public string? Notes { get; set; }
}
