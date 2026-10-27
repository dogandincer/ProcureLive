using Microsoft.AspNetCore.SignalR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Auctions;

namespace ProcureLive.WebApi.Hubs;

public sealed class SignalRAuctionNotificationService : IAuctionNotificationService
{
    private readonly IHubContext<AuctionHub> _hubContext;

    public SignalRAuctionNotificationService(IHubContext<AuctionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BidPlacedAsync(Guid auctionId, BidDto bid, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(AuctionHub.GroupName(auctionId))
            .SendAsync("BidPlaced", bid, cancellationToken);
    }

    public Task AuctionExtendedAsync(Guid auctionId, DateTime newEndAt, string reason, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(AuctionHub.GroupName(auctionId))
            .SendAsync("AuctionExtended", new { auctionId, newEndAt, reason }, cancellationToken);
    }

    public Task AuctionStartedAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(AuctionHub.GroupName(auctionId))
            .SendAsync("AuctionStarted", new { auctionId }, cancellationToken);
    }

    public Task AuctionEndedAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(AuctionHub.GroupName(auctionId))
            .SendAsync("AuctionEnded", new { auctionId }, cancellationToken);
    }

    public Task ItemAwardedAsync(Guid auctionId, Guid auctionItemId, Guid winningCompanyId, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(AuctionHub.GroupName(auctionId))
            .SendAsync("ItemAwarded", new { auctionId, auctionItemId, winningCompanyId }, cancellationToken);
    }

    public Task AuctionStatusChangedAsync(Guid auctionId, string status, string? reason, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(AuctionHub.GroupName(auctionId))
            .SendAsync("AuctionStatusChanged", new { auctionId, status, reason }, cancellationToken);
    }
}
