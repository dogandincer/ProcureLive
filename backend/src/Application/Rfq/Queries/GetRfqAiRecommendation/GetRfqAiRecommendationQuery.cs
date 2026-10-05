using MediatR;
using ProcureLive.Contracts.Rfq;

namespace ProcureLive.Application.Rfq.Queries.GetRfqAiRecommendation;

public sealed record GetRfqAiRecommendationQuery(Guid RfqProcessId)
    : IRequest<RfqAiRecommendationDto>;
