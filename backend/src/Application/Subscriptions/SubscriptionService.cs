using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Subscriptions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Subscriptions;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public SubscriptionService(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public Task<bool> IsPlusAsync(Guid companyId, CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;
        return _context.CompanySubscriptions.AnyAsync(s =>
            s.CompanyId == companyId && s.Plan == SubscriptionPlan.Plus
            && s.Status == SubscriptionStatus.Active && s.ExpiresAt > now, cancellationToken);
    }

    public Task<CompanySubscription?> GetActivePlusAsync(Guid companyId, CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;
        return _context.CompanySubscriptions
            .Where(s => s.CompanyId == companyId && s.Plan == SubscriptionPlan.Plus
                && s.Status == SubscriptionStatus.Active && s.ExpiresAt > now)
            .OrderByDescending(s => s.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
