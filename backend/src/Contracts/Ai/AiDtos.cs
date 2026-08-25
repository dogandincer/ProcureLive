namespace ProcureLive.Contracts.Ai;

/// <summary>
/// Aggregated historical signal, computed from AuctionAnalyticsView/SupplierPerformanceView, that a
/// real model (or today's heuristic placeholder) consumes to recommend an auction model.
/// </summary>
public sealed record AuctionModelFeatureSetDto(
    Guid? CategoryId,
    int HistoricalAuctionCount,
    decimal AverageBidCount,
    decimal AverageUniqueBidderCount,
    decimal? AveragePriceChangePercentage,
    decimal AverageExtensionCount,
    decimal? AverageInvitedSupplierRfiScore,
    decimal? AverageInvitedSupplierWinRate,
    int EstimatedParticipantCount,
    int EstimatedItemCount);

public sealed record AuctionModelRecommendationRequest(
    Guid? CategoryId,
    IReadOnlyCollection<Guid>? CandidateSupplierCompanyIds,
    int EstimatedParticipantCount,
    int EstimatedItemCount);

public sealed record AuctionModelRecommendationDto(
    string RecommendedAuctionType,
    double ConfidenceScore,
    string Rationale,
    AuctionModelFeatureSetDto FeaturesUsed);
