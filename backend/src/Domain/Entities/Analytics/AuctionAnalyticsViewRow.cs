namespace ProcureLive.Domain.Entities.Analytics;

/// <summary>Read-only projection of the "AuctionAnalyticsView" database view (see AddAnalyticsViews migration).</summary>
public class AuctionAnalyticsViewRow
{
    public Guid AuctionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int AuctionType { get; set; }

    public int Status { get; set; }

    public Guid OwnerCompanyId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int TotalBidCount { get; set; }

    public int UniqueBidderCount { get; set; }

    public int ExtensionCount { get; set; }

    public decimal? FirstBidAmount { get; set; }

    public DateTime? FirstBidAt { get; set; }

    public decimal? LastBidAmount { get; set; }

    public decimal? PriceChangePercentage { get; set; }

    public double? MinutesToFirstBid { get; set; }
}
