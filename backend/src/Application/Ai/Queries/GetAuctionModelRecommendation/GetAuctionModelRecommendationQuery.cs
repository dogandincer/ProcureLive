using MediatR;
using ProcureLive.Contracts.Ai;

namespace ProcureLive.Application.Ai.Queries.GetAuctionModelRecommendation;

public sealed record GetAuctionModelRecommendationQuery(
    Guid? CategoryId,
    IReadOnlyCollection<Guid>? CandidateSupplierCompanyIds,
    int EstimatedParticipantCount,
    int EstimatedItemCount) : IRequest<AuctionModelRecommendationDto>;
