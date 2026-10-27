using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Subscriptions;
using ProcureLive.Domain.Entities.Subscriptions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Subscriptions.Queries;

/// <summary>Oturumdaki kullanıcının carisinin planı ve katılım kotası (Standart: <see cref="SupplierAccessContext.StandardJoinLimit"/>).</summary>
public sealed record GetMySubscriptionQuery : IRequest<MySubscriptionDto>;

public sealed class GetMySubscriptionQueryHandler : IRequestHandler<GetMySubscriptionQuery, MySubscriptionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISupplierAccessService _accessService;
    private readonly IIdentityService _identityService;

    public GetMySubscriptionQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISubscriptionService subscriptionService,
        ISupplierAccessService accessService,
        IIdentityService identityService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
        _accessService = accessService;
        _identityService = identityService;
    }

    public async Task<MySubscriptionDto> Handle(GetMySubscriptionQuery request, CancellationToken cancellationToken)
    {
        Guid companyId = _currentUserService.CompanyId
            ?? throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");

        string companyName = await _context.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Company", companyId);

        CompanySubscription? plus = await _subscriptionService.GetActivePlusAsync(companyId, cancellationToken);
        SupplierAccessContext access = await _accessService.GetContextAsync(companyId, cancellationToken);
        int userCount = (await _identityService.ListUsersAsync(null, companyId, 1, 1, null, cancellationToken)).TotalCount;

        return new MySubscriptionDto(
            companyId,
            companyName,
            (plus is null ? SubscriptionPlan.Standard : SubscriptionPlan.Plus).ToString(),
            plus is not null,
            plus?.SeatBand.ToString(),
            plus?.PriceUsd,
            plus?.ExpiresAt,
            access.JoinedCount,
            SupplierAccessContext.StandardJoinLimit,
            access.RemainingJoins,
            userCount);
    }
}
