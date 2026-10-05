namespace ProcureLive.Contracts.Monitoring;

public sealed record MonitoringStatsDto(
    IReadOnlyDictionary<string, int> AuctionCountByStatus,
    IReadOnlyDictionary<string, int> RfqCountByStatus,
    int TotalBidsToday,
    int TotalBidsThisWeek,
    IReadOnlyCollection<LiveAuctionSummaryDto> LiveAuctions,
    IReadOnlyCollection<RecentBidDto> RecentBids);

public sealed record LiveAuctionSummaryDto(
    Guid AuctionId,
    string Title,
    int BidCount,
    decimal? BestBidAmount,
    string? BestBidCurrencyCode,
    string? LeadingCompanyName,
    DateTime EndsAt);

public sealed record RecentBidDto(
    Guid BidId,
    string AuctionTitle,
    string CompanyName,
    decimal Amount,
    string CurrencyCode,
    DateTime BidTime);
