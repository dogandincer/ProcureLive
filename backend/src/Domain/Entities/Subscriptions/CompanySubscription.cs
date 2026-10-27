using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Subscriptions;

/// <summary>
/// Firma (cari) bazlı abonelik kaydı. Plus erişimi = Status Active ve ExpiresAt gelecekte olan bir Plus kaydı.
/// Satın alımlar geçmişi korunur (yükseltmede eski kayıt Cancelled olur, yenisi açılır).
/// </summary>
public class CompanySubscription : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public SubscriptionPlan Plan { get; set; }

    public SeatBand SeatBand { get; set; }

    public decimal PriceUsd { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>Ödeme sağlayıcısının işlem referansı (şimdilik mock gateway üretir).</summary>
    public string PaymentReference { get; set; } = string.Empty;

    public Guid PurchasedByUserId { get; set; }
}
