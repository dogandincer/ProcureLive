using MediatR;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Subscriptions;

namespace ProcureLive.Application.Subscriptions.Queries;

public sealed record GetSubscriptionPlansQuery : IRequest<SubscriptionPlansDto>;

public sealed class GetSubscriptionPlansQueryHandler : IRequestHandler<GetSubscriptionPlansQuery, SubscriptionPlansDto>
{
    public Task<SubscriptionPlansDto> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        List<SeatBandPriceDto> bands = SubscriptionPricing.Bands
            .Select(b => new SeatBandPriceDto(b.Band.ToString(), b.MinUsers, b.MaxUsers, b.PriceUsd))
            .ToList();

        return Task.FromResult(new SubscriptionPlansDto(
            bands, SubscriptionPricing.PlanDurationMonths, SupplierAccessContext.StandardJoinLimit));
    }
}
