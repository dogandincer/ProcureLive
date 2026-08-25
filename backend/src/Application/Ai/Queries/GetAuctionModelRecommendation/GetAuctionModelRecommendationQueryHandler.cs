using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Ai;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Application.Ai.Queries.GetAuctionModelRecommendation;

/// <summary>
/// Builds the feature vector from historical AuctionAnalyticsView/SupplierPerformanceView data and
/// hands it to IAuctionModelRecommender. This handler is the stable "data preparation" contract a
/// future trained model plugs into — only the recommender implementation is expected to change.
/// </summary>
public sealed class GetAuctionModelRecommendationQueryHandler
    : IRequestHandler<GetAuctionModelRecommendationQuery, AuctionModelRecommendationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuctionModelRecommender _recommender;

    public GetAuctionModelRecommendationQueryHandler(IApplicationDbContext context, IAuctionModelRecommender recommender)
    {
        _context = context;
        _recommender = recommender;
    }

    public async Task<AuctionModelRecommendationDto> Handle(
        GetAuctionModelRecommendationQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Auction> auctionScope = _context.Auctions.AsQueryable();

        if (request.CategoryId.HasValue)
        {
            auctionScope = auctionScope.Where(a => a.Items.Any(i => i.Product != null && i.Product.CategoryId == request.CategoryId.Value));
        }

        List<Guid> relevantAuctionIds = await auctionScope.Select(a => a.Id).ToListAsync(cancellationToken);

        List<Domain.Entities.Analytics.AuctionAnalyticsViewRow> analytics = relevantAuctionIds.Count > 0
            ? await _context.AuctionAnalytics.Where(a => relevantAuctionIds.Contains(a.AuctionId)).ToListAsync(cancellationToken)
            : await _context.AuctionAnalytics.ToListAsync(cancellationToken);

        decimal averageBidCount = analytics.Count > 0 ? (decimal)analytics.Average(a => a.TotalBidCount) : 0;
        decimal averageUniqueBidders = analytics.Count > 0 ? (decimal)analytics.Average(a => a.UniqueBidderCount) : 0;
        decimal averageExtensions = analytics.Count > 0 ? (decimal)analytics.Average(a => a.ExtensionCount) : 0;

        List<decimal> priceChanges = analytics.Where(a => a.PriceChangePercentage.HasValue).Select(a => a.PriceChangePercentage!.Value).ToList();
        decimal? averagePriceChange = priceChanges.Count > 0 ? priceChanges.Average() : null;

        decimal? averageSupplierRfiScore = null;
        decimal? averageSupplierWinRate = null;

        if (request.CandidateSupplierCompanyIds is { Count: > 0 })
        {
            List<Domain.Entities.Analytics.SupplierPerformanceViewRow> supplierRows = await _context.SupplierPerformance
                .Where(s => request.CandidateSupplierCompanyIds.Contains(s.CompanyId))
                .ToListAsync(cancellationToken);

            List<decimal> rfiScores = supplierRows.Where(s => s.AverageRfiScore.HasValue).Select(s => s.AverageRfiScore!.Value).ToList();
            List<decimal> winRates = supplierRows.Where(s => s.AuctionWinRatePercentage.HasValue).Select(s => s.AuctionWinRatePercentage!.Value).ToList();

            averageSupplierRfiScore = rfiScores.Count > 0 ? rfiScores.Average() : null;
            averageSupplierWinRate = winRates.Count > 0 ? winRates.Average() : null;
        }

        var features = new AuctionModelFeatureSetDto(
            request.CategoryId,
            analytics.Count,
            averageBidCount,
            averageUniqueBidders,
            averagePriceChange,
            averageExtensions,
            averageSupplierRfiScore,
            averageSupplierWinRate,
            request.EstimatedParticipantCount,
            request.EstimatedItemCount);

        (string recommendedType, double confidence, string rationale) = await _recommender.RecommendAsync(features, cancellationToken);

        return new AuctionModelRecommendationDto(recommendedType, confidence, rationale, features);
    }
}
