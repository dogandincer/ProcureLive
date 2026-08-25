using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Ai.Queries.GetAuctionModelRecommendation;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Ai;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Policy = Policies.RequireInternalUser)]
public sealed class AiController : ControllerBase
{
    private readonly ISender _sender;

    public AiController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// "En uygun ihale modeli" önerisi — geçmiş AuctionAnalyticsView/SupplierPerformanceView
    /// verilerinden hazırlanan özellik vektörünü IAuctionModelRecommender'a besler.
    /// Bugün kural tabanlı bir placeholder (HeuristicAuctionModelRecommender) çalışıyor;
    /// eğitimli bir model geldiğinde yalnızca o Infrastructure implementasyonu değişecek.
    /// </summary>
    [HttpPost("auction-model-recommendation")]
    public async Task<ActionResult<AuctionModelRecommendationDto>> RecommendAuctionModel(
        AuctionModelRecommendationRequest request, CancellationToken cancellationToken)
    {
        AuctionModelRecommendationDto result = await _sender.Send(
            new GetAuctionModelRecommendationQuery(
                request.CategoryId, request.CandidateSupplierCompanyIds,
                request.EstimatedParticipantCount, request.EstimatedItemCount),
            cancellationToken);

        return Ok(result);
    }
}
