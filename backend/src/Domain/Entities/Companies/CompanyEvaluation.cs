using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Companies;

/// <summary>
/// Şirket-geneli (süreçten bağımsız) tedarikçi karnesi. RFI'ya bağlı tek-puanlı
/// <see cref="ProcureLive.Domain.Entities.Rfi.SupplierEvaluation"/>'dan farklı olarak çok-kriterli
/// ve bir cariye ait tüm değerlendirmeler ortalanarak genel bir cari puanı üretir.
/// Kriterler 1-5 arası; genel puan kurumsal ağırlıklarla hesaplanır.
/// </summary>
public class CompanyEvaluation : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid EvaluatedByUserId { get; set; }

    /// <summary>Kalite (1-5).</summary>
    public int QualityScore { get; set; }

    /// <summary>Teslimat / zamanında teslim (1-5).</summary>
    public int DeliveryScore { get; set; }

    /// <summary>İletişim / yanıt verebilirlik (1-5).</summary>
    public int CommunicationScore { get; set; }

    /// <summary>Fiyat / maliyet rekabetçiliği (1-5).</summary>
    public int PriceScore { get; set; }

    /// <summary>Uyum / belge eksiksizliği (1-5).</summary>
    public int ComplianceScore { get; set; }

    /// <summary>Kurumsal ağırlıklarla hesaplanmış genel puan (1-5, iki ondalık).</summary>
    public decimal OverallScore { get; set; }

    public string? Comments { get; set; }

    public DateTime EvaluatedAt { get; set; }

    /// <summary>İzlenebilirlik: değerlendirmenin kaynağı ("Manual" | "Auction" | "Rfq" | "Rfi").</summary>
    public string? SourceType { get; set; }

    public Guid? SourceProcessId { get; set; }
}
