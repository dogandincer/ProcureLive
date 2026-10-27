using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Domain.Entities.Auctions;

/// <summary>
/// Bir cariye belirli bir ihale için verilen süreli davet linki (/invite?token=...).
/// Linkle yeni hesap açma tek kullanımlıktır (UsedAt); mevcut hesabı olan aynı cari kullanıcıları
/// süre dolana kadar linki "sahiplenebilir" (claim).
/// </summary>
public class InvitationToken : AuditableEntity
{
    public string Token { get; set; } = string.Empty;

    public Guid AuctionId { get; set; }

    public Auction Auction { get; set; } = null!;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    /// <summary>Opsiyonel kilit: doluysa yalnızca bu e-posta ile kayıt olunabilir.</summary>
    public string? Email { get; set; }

    public DateTime ExpiresAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime? UsedAt { get; set; }

    public Guid? RegisteredUserId { get; set; }
}
