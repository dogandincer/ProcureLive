using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Auctions;

public class AuctionExtensionLog : BaseEntity
{
    public Guid AuctionId { get; set; }

    public Auction Auction { get; set; } = null!;

    public Guid? TriggeredByBidId { get; set; }

    public Bid? TriggeredByBid { get; set; }

    public DateTime OriginalEndAt { get; set; }

    public DateTime NewEndAt { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid? ExtendedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
