using System.Globalization;
using Microsoft.Extensions.Caching.Distributed;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services;

/// <summary>Redis-backed cache of the current best bid per auction item (fast reads, avoids hitting Postgres per request).</summary>
public sealed class AuctionCacheService : IAuctionCacheService
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(6)
    };

    private readonly IDistributedCache _cache;

    public AuctionCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<decimal?> GetBestAmountAsync(Guid auctionItemId, CancellationToken cancellationToken)
    {
        string? value = await _cache.GetStringAsync(BuildKey(auctionItemId), cancellationToken);

        if (value is null)
        {
            return null;
        }

        decimal amount = 0m;
        bool parsed = decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out amount);

        return parsed ? amount : null;
    }

    public Task SetBestAmountAsync(Guid auctionItemId, decimal amount, CancellationToken cancellationToken)
    {
        string value = amount.ToString(CultureInfo.InvariantCulture);
        return _cache.SetStringAsync(BuildKey(auctionItemId), value, CacheOptions, cancellationToken);
    }

    public Task RemoveAsync(Guid auctionItemId, CancellationToken cancellationToken)
    {
        return _cache.RemoveAsync(BuildKey(auctionItemId), cancellationToken);
    }

    private static string BuildKey(Guid auctionItemId) => $"auction-item-best:{auctionItemId}";
}
