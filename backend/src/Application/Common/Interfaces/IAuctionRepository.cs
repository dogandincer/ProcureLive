using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Application.Common.Interfaces;

/// <summary>Interface defined in Phase 1 alongside the schema; implemented/consumed starting Phase 2.</summary>
public interface IAuctionRepository : IRepository<Auction>
{
    Task<Auction?> GetWithItemsAndInvitationsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsCompanyInvitedAsync(Guid auctionId, Guid companyId, CancellationToken cancellationToken);
}
