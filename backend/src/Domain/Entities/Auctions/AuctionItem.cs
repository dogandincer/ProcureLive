using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Auctions;

public class AuctionItem : BaseEntity
{
    public Guid AuctionId { get; set; }

    public Auction Auction { get; set; } = null!;

    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public UnitOfMeasure UnitOfMeasure { get; set; }

    public decimal? TargetPrice { get; set; }

    /// <summary>Satır bazlı istenen teslim tarihi (ihale genelinden farklı olabilir).</summary>
    public DateTime? RequestedDeliveryDate { get; set; }

    /// <summary>Satır bazlı teknik şartname / açıklama.</summary>
    public string? Specification { get; set; }

    public int SortOrder { get; set; }

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}
