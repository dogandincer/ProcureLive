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

    public AuctionHub(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Mirrors GetAuctionById's read-access rule (AuctionAccessHandler): Admin/Buyer always join;
    /// a Supplier only joins if invited (any status but Declined) — otherwise they could eavesdrop
    /// on another company's live bid broadcasts, which the REST API already refuses to reveal.
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

            bool isInvited = parsed && await _context.AuctionInvitations.AnyAsync(i =>
                i.AuctionId == auctionId && i.CompanyId == companyId && i.Status != InvitationStatus.Declined);

            if (!isInvited)
            {
                return;
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(auctionId));
    }

    public async Task LeaveAuctionGroup(Guid auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(auctionId));
    }

    public static string GroupName(Guid auctionId) => $"auction:{auctionId}";
}
