using ProcureLive.Domain.Entities.Subscriptions;

namespace ProcureLive.Application.Common.Interfaces;

public interface ISubscriptionService
{
    Task<bool> IsPlusAsync(Guid companyId, CancellationToken cancellationToken);

    Task<CompanySubscription?> GetActivePlusAsync(Guid companyId, CancellationToken cancellationToken);
}
