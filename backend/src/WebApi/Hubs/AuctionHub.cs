using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

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
    public async Task JoinAuctionGroup(Guid auctionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(auctionId));
    }

    public async Task LeaveAuctionGroup(Guid auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(auctionId));
    }

    public static string GroupName(Guid auctionId) => $"auction:{auctionId}";
}
