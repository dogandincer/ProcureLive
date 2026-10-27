using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Security;

public sealed class SupplierAccessService : ISupplierAccessService
{
    /// <summary>Kotaya sayılan ("katılınmış") davet durumları; arama ekranındaki katılımcı sayısı da bunu kullanır.</summary>
    public static readonly InvitationStatus[] JoinedStatuses =
    {
        InvitationStatus.Accepted, InvitationStatus.Participating, InvitationStatus.Won, InvitationStatus.Lost
    };

    private readonly IApplicationDbContext _context;
    private readonly ISubscriptionService _subscriptions;

    public SupplierAccessService(IApplicationDbContext context, ISubscriptionService subscriptions)
    {
        _context = context;
        _subscriptions = subscriptions;
    }

    public async Task<SupplierAccessContext> GetContextAsync(Guid companyId, CancellationToken cancellationToken)
    {
        // Tek sorgu: carinin tüm davetleri + ihale sahibi.
        var invitations = await _context.AuctionInvitations
            .Where(i => i.CompanyId == companyId)
            .Select(i => new { i.AuctionId, i.Status, i.Origin, OwnerId = i.Auction.OwnerCompanyId })
            .ToListAsync(cancellationToken);

        bool isPlus = await _subscriptions.IsPlusAsync(companyId, cancellationToken);

        var notDeclined = invitations.Where(i => i.Status != InvitationStatus.Declined).ToList();

        return new SupplierAccessContext
        {
            CompanyId = companyId,
            IsPlus = isPlus,
            InvitedAuctionIds = notDeclined.Select(i => i.AuctionId).ToHashSet(),
            JoinedAuctionIds = invitations.Where(i => JoinedStatuses.Contains(i.Status)).Select(i => i.AuctionId).ToHashSet(),
            DeclinedAuctionIds = invitations.Where(i => i.Status == InvitationStatus.Declined).Select(i => i.AuctionId).ToHashSet(),
            // Kendiliğinden katılım (SelfJoin) sahibi "davet eden" yapmaz; yalnızca gerçek davetler.
            InvitingOwnerIds = notDeclined.Where(i => i.Origin != InvitationOrigin.SelfJoin).Select(i => i.OwnerId).ToHashSet(),
        };
    }
}
