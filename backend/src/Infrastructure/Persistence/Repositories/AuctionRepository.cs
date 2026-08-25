using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Repositories;

/// <summary>Implemented in Phase 1 alongside the schema; consumed starting Phase 2 (live auction engine).</summary>
public sealed class AuctionRepository : RepositoryBase<Auction>, IAuctionRepository
{
    public AuctionRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<Auction?> GetWithItemsAndInvitationsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await DbSet
            .Include(a => a.Items)
            .Include(a => a.Invitations)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> IsCompanyInvitedAsync(Guid auctionId, Guid companyId, CancellationToken cancellationToken)
    {
        return await Context.AuctionInvitations
            .AnyAsync(i => i.AuctionId == auctionId && i.CompanyId == companyId, cancellationToken);
    }
}
