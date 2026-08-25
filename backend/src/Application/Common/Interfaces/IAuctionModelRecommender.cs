using ProcureLive.Contracts.Ai;

namespace ProcureLive.Application.Common.Interfaces;

/// <summary>
/// Pluggable "which auction model fits this context best" recommender (İngiliz/Hollanda/Ters Mühürlü/
/// Kapalı Zarf). Phase 4 ships a rule-based placeholder (HeuristicAuctionModelRecommender) that consumes
/// the same AuctionModelFeatureSetDto a trained model would; swapping in ML.NET or an external scoring
/// service later means replacing only this one Infrastructure implementation — nothing above it changes.
/// </summary>
public interface IAuctionModelRecommender
{
    Task<(string RecommendedAuctionType, double ConfidenceScore, string Rationale)> RecommendAsync(
        AuctionModelFeatureSetDto features,
        CancellationToken cancellationToken);
}
