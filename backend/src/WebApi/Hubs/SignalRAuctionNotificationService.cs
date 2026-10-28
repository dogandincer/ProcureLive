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

    public async Task BidPlacedAsync(Guid auctionId, BidDto bid, bool hideAmountFromSuppliers, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(AuctionHub.InternalGroupName(auctionId))
            .SendAsync("BidPlaced", bid with { IsMine = false }, cancellationToken);

        // Tedarikçiler rakibin kimliğini hiç, kapalı zarfta tutarı da görmez; kendi teklifini istemci bid.id ile ayırt eder.
        var anonymous = bid with { CompanyId = Guid.Empty, CompanyName = string.Empty, Amount = hideAmountFromSuppliers ? 0 : bid.Amount, IsMine = false };
        await _hubContext.Clients.Group(AuctionHub.GroupName(auctionId))
            .SendAsync("BidPlaced", anonymous, cancellationToken);
    }

    public Task AuctionExtendedAsync(Guid auctionId, DateTime newEndAt, string reason, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Groups(AuctionHub.AllGroups(auctionId))
            .SendAsync("AuctionExtended", new { auctionId, newEndAt, reason }, cancellationToken);
    }

    public Task AuctionStartedAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Groups(AuctionHub.AllGroups(auctionId))
            .SendAsync("AuctionStarted", new { auctionId }, cancellationToken);
    }

    public Task AuctionEndedAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Groups(AuctionHub.AllGroups(auctionId))
            .SendAsync("AuctionEnded", new { auctionId }, cancellationToken);
    }

    public Task ItemAwardedAsync(Guid auctionId, Guid auctionItemId, Guid winningCompanyId, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Groups(AuctionHub.AllGroups(auctionId))
            .SendAsync("ItemAwarded", new { auctionId, auctionItemId, winningCompanyId }, cancellationToken);
    }

    public Task AuctionStatusChangedAsync(Guid auctionId, string status, string? reason, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Groups(AuctionHub.AllGroups(auctionId))
            .SendAsync("AuctionStatusChanged", new { auctionId, status, reason }, cancellationToken);
    }
}
