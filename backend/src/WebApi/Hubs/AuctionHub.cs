using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Hubs;

/// <summary>
/// Realtime channel for live auctions. Clients join the group for the auction they are viewing;
/// server-side command handlers broadcast BidPlaced/AuctionExtended/AuctionStarted/AuctionEnded/
/// ItemAwarded into that group via IAuctionNotificationService (SignalRAuctionNotificationService).
/// JWT auth is read from the access_token query string (configured in Program.cs) since browser
/// WebSocket connections cannot set an Authorization header.
/// </summary>
[Authorize]
public sealed class AuctionHub : Hub
{
    private readonly IApplicationDbContext _context;
    private readonly ISupplierAccessService _accessService;

    public AuctionHub(IApplicationDbContext context, ISupplierAccessService accessService)
    {
        _context = context;
        _accessService = accessService;
    }

    /// <summary>
    /// Mirrors GetAuctionById's read-access rule (SupplierAccessContext.Evaluate): Admin/Buyer always join;
    /// a Supplier only joins with Full access (visible, not declined, within the Standard quota) — otherwise
    /// they could eavesdrop on live bid broadcasts the REST API only returns as a restricted view.
    /// </summary>
    public async Task JoinAuctionGroup(Guid auctionId)
    {
        if (Context.User is null)
        {
            return;
        }

        bool isInternalUser = Context.User.IsInRole(Roles.Admin) || Context.User.IsInRole(Roles.Buyer);

        if (!isInternalUser)
        {
            string? companyIdClaim = Context.User.FindFirstValue(ProcureLiveClaimTypes.CompanyId);

            Guid companyId = Guid.Empty;
            bool parsed = companyIdClaim is not null && Guid.TryParse(companyIdClaim, out companyId);

            if (!parsed)
            {
                return;
            }

            var auction = await _context.Auctions
                .Where(a => a.Id == auctionId)
                .Select(a => new { a.Id, a.Status, a.OwnerCompanyId, a.IsListedForPlus, OwnerLists = a.OwnerCompany.ListTendersForPlus })
                .FirstOrDefaultAsync(Context.ConnectionAborted);

            if (auction is null)
            {
                return;
            }

            // Kısıtlı (kota dolu / yetkisiz) görüntüleyen canlı teklif yayınını dinleyemez — REST ile aynı kural.
            SupplierAccessContext access = await _accessService.GetContextAsync(companyId, Context.ConnectionAborted);
            var facts = new AuctionAccessFacts(auction.Id, auction.Status, auction.OwnerCompanyId, auction.IsListedForPlus, auction.OwnerLists);
            if (access.Evaluate(facts) != AuctionAccessLevel.Full)
            {
                return;
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, isInternalUser ? InternalGroupName(auctionId) : GroupName(auctionId));
    }

    public async Task LeaveAuctionGroup(Guid auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(auctionId));
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, InternalGroupName(auctionId));
    }

    /// <summary>Tedarikçi bağlantıları (teklifler anonim gelir).</summary>
    public static string GroupName(Guid auctionId) => $"auction:{auctionId}";

    /// <summary>İç kullanıcı bağlantıları (teklifler firma adıyla gelir).</summary>
    public static string InternalGroupName(Guid auctionId) => $"auction:{auctionId}:internal";

    public static IReadOnlyList<string> AllGroups(Guid auctionId) => [GroupName(auctionId), InternalGroupName(auctionId)];
}
