using ProcureLive.Contracts.Auctions;

namespace ProcureLive.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the SignalR AuctionHub so Application never references ASP.NET Core SignalR types.
/// Implemented in WebApi (where the Hub lives) and injected via DI.
/// </summary>
public interface IAuctionNotificationService
{
    Task BidPlacedAsync(Guid auctionId, BidDto bid, CancellationToken cancellationToken);

    Task AuctionExtendedAsync(Guid auctionId, DateTime newEndAt, string reason, CancellationToken cancellationToken);

    Task AuctionStartedAsync(Guid auctionId, CancellationToken cancellationToken);

    Task AuctionEndedAsync(Guid auctionId, CancellationToken cancellationToken);

    Task ItemAwardedAsync(Guid auctionId, Guid auctionItemId, Guid winningCompanyId, CancellationToken cancellationToken);

    /// <summary>Elle yapılan durum değişikliği (başlat/duraklat/devam/bitir/iptal...) — açık sayfalar kendini yeniler.</summary>
    Task AuctionStatusChangedAsync(Guid auctionId, string status, string? reason, CancellationToken cancellationToken);
}
