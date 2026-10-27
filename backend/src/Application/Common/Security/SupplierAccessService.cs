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

        var company = await _context.Companies
            .Where(c => c.Id == companyId)
            .Select(c => new
            {
                c.Status,
                c.WorkspaceId,
                PrimaryFirmId = c.Workspace != null ? c.Workspace.FirmId : null,
                Visible = c.VisibleWorkspaces.Select(v => new { v.WorkspaceId, v.Workspace.FirmId }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var approvedWorkspaces = new HashSet<Guid>();
        var approvedFirms = new HashSet<Guid>();
        if (company is not null)
        {
            if (company.WorkspaceId is Guid w) approvedWorkspaces.Add(w);
            if (company.PrimaryFirmId is Guid f) approvedFirms.Add(f);
            foreach (var v in company.Visible)
            {
                approvedWorkspaces.Add(v.WorkspaceId);
                if (v.FirmId is Guid vf) approvedFirms.Add(vf);
            }
        }

        // İhale başına en son talep.
        var requests = await _context.ParticipationRequests
            .Where(r => r.CompanyId == companyId)
            .Select(r => new { r.AuctionId, r.Status, r.CreatedAt })
            .ToListAsync(cancellationToken);
        Dictionary<Guid, ParticipationRequestStatus> requestStatuses = requests
            .GroupBy(r => r.AuctionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAt).First().Status);

        return new SupplierAccessContext
        {
            CompanyId = companyId,
            IsPlus = isPlus,
            InvitedAuctionIds = notDeclined.Select(i => i.AuctionId).ToHashSet(),
            JoinedAuctionIds = invitations.Where(i => JoinedStatuses.Contains(i.Status)).Select(i => i.AuctionId).ToHashSet(),
            DeclinedAuctionIds = invitations.Where(i => i.Status == InvitationStatus.Declined).Select(i => i.AuctionId).ToHashSet(),
            // Yalnızca gerçek davetler (Direct/InviteLink) sahibi "davet eden" yapar. Kendiliğinden katılım (SelfJoin) ve
            // kabul edilmiş katılım talebi (Request) firmanın listelemeye kapalı diğer ihalelerini açmaz.
            InvitingOwnerIds = notDeclined
                .Where(i => i.Origin is InvitationOrigin.Direct or InvitationOrigin.InviteLink)
                .Select(i => i.OwnerId).ToHashSet(),
            ApprovedWorkspaceIds = approvedWorkspaces,
            ApprovedFirmIds = approvedFirms,
            IsCompanyActive = company is not null && company.Status != CompanyStatus.Passive,
            RequestStatuses = requestStatuses,
        };
    }
}
