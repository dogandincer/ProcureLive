using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Auctions;

/// <summary>
/// İhaleye ait doküman (teknik çizim, şartname vb.). Dosya depolama henüz simüle;
/// yalnızca harici/CDN URL + meta tutulur (gerçek blob storage sonraya bırakıldı).
/// </summary>
public class AuctionAttachment : AuditableEntity
{
    public Guid AuctionId { get; set; }

    public Auction Auction { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }
}
