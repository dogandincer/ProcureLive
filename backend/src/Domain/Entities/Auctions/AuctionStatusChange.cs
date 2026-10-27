using ProcureLive.Domain.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Auctions;

/// <summary>
/// İhale durum geçmişi: elle (admin) veya otomatik (zamanlayıcı, son kalemin kazananı) yapılan her durum
/// değişikliği bir satırdır. Otomatik değişikliklerde ChangedByUserId zamanlayıcı için boştur.
/// </summary>
public class AuctionStatusChange : BaseEntity
{
    public Guid AuctionId { get; set; }

    public Auction Auction { get; set; } = null!;

    public AuctionStatus FromStatus { get; set; }

    public AuctionStatus ToStatus { get; set; }

    public string? Reason { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public bool IsAutomatic { get; set; }

    public DateTime ChangedAt { get; set; }
}
