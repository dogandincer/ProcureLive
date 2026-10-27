using MediatR;
using ProcureLive.Contracts.Rfq;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfq.Queries.GetRfqAiRecommendation;

public sealed record GetRfqAiRecommendationQuery(Guid RfqProcessId)
    : IRequest<RfqAiRecommendationDto>, IRfqScopedRequest;
