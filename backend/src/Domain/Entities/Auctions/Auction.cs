using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Finance;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Auctions;

public class Auction : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>İnsan-okur ihale referans kodu (ör. IH-2026-00042). Oluşturulurken otomatik üretilebilir.</summary>
    public string? ReferenceCode { get; set; }

    public string? Description { get; set; }

    public AuctionType AuctionType { get; set; }

    public AuctionStatus Status { get; set; } = AuctionStatus.Draft;

    public Guid OwnerCompanyId { get; set; }

    /// <summary>İhalenin işyeri (sahip firmanın işyerlerinden). Cari onayı bu işyeri üzerinden yapılır.</summary>
    public Guid? WorkspaceId { get; set; }

    /// <summary>Sorumlu satınalmacı (varsayılan: ihaleyi açan iç kullanıcı) — katılım talepleri ona düşer.</summary>
    public Guid? ResponsibleUserId { get; set; }

    public Company OwnerCompany { get; set; } = null!;

    public Guid CurrencyId { get; set; }

    public Currency Currency { get; set; } = null!;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public DateTime? ActualStartAt { get; set; }

    public DateTime? ActualEndAt { get; set; }

    /// <summary>Duraklatılma anı (Paused iken dolu). Devam ettirilince duraklama süresi EndAt'e eklenir.</summary>
    public DateTime? PausedAt { get; set; }

    /// <summary>Set once the "starting soon" reminder has gone out, so the lifecycle poller doesn't re-send it every tick.</summary>
    public DateTime? StartingReminderSentAt { get; set; }

    public decimal? ReservePrice { get; set; }

    public decimal? MinDecrementAmount { get; set; }

    public decimal? MinDecrementPercentage { get; set; }

    /// <summary>Anti-sniping tetik penceresi: EndAt'e bu kadar dakika kala teklif gelirse süre uzatılır (X).</summary>
    public int ExtensionRuleMinutes { get; set; }

    /// <summary>Anti-sniping: tetiklendiğinde EndAt'in uzatılacağı dakika (Y). Null ise tetik penceresi kadar uzatılır.</summary>
    public int? ExtensionDurationMinutes { get; set; }

    /// <summary>Anti-sniping: StartAt'e eklenebilecek toplam uzatma tavanı (Z, dakika). Null ise sınırsız.</summary>
    public int? MaxExtensionMinutes { get; set; }

    /// <summary>Teklif şartı: ödeme vadesi.</summary>
    public PaymentTerm? PaymentTerm { get; set; }

    /// <summary>Teklif şartı: teslimat şekli (Incoterms).</summary>
    public DeliveryTerm? DeliveryTerm { get; set; }

    /// <summary>Tedarikçi muadil (alternatif) ürün teklif edebilir mi?</summary>
    public bool AllowAlternativeProducts { get; set; }

    /// <summary>Tedarikçi kalemlerin yalnızca bir kısmına teklif verebilir mi?</summary>
    public bool AllowPartialBids { get; set; }

    /// <summary>Plus kullanıcıların aramalarında listelenebilir (sahibi firmanın ListTendersForPlus'ı da açık olmalı).</summary>
    public bool IsListedForPlus { get; set; }

    public ICollection<AuctionItem> Items { get; set; } = new List<AuctionItem>();

    public ICollection<AuctionInvitation> Invitations { get; set; } = new List<AuctionInvitation>();

    public ICollection<AuctionExtensionLog> ExtensionLogs { get; set; } = new List<AuctionExtensionLog>();

    public ICollection<AuctionAttachment> Attachments { get; set; } = new List<AuctionAttachment>();

    public ICollection<AuctionStatusChange> StatusChanges { get; set; } = new List<AuctionStatusChange>();
}
