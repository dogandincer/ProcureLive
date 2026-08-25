namespace ProcureLive.Application.Common.Interfaces;

/// <summary>Redis-backed cache of the current best bid per auction item, to avoid hammering Postgres on every read.</summary>
public interface IAuctionCacheService
{
    Task<decimal?> GetBestAmountAsync(Guid auctionItemId, CancellationToken cancellationToken);

    Task SetBestAmountAsync(Guid auctionItemId, decimal amount, CancellationToken cancellationToken);

    Task RemoveAsync(Guid auctionItemId, CancellationToken cancellationToken);
}
