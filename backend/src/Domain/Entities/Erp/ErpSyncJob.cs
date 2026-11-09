using ProcureLive.Domain.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Erp;

/// <summary>
/// ERP'ye yazma kuyruğu (giden kutusu). XML gönderim anında güncel veriden üretilir ve kayda yazılır; böylece hem simülasyonda
/// hem canlıda Admin tam olarak neyin gönderildiğini (gönderileceğini) görür. Denetim kaydına yazılmaz (gürültü olmasın).
/// </summary>
public class ErpSyncJob : BaseEntity
{
    public ErpSyncKind Kind { get; set; }
    public ErpSyncStatus Status { get; set; } = ErpSyncStatus.Pending;

    public Guid? AuctionId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>Kayıt ekranında görünen kısa açıklama (ör. "IH-2026-0004 · Ege Bilişim").</summary>
    public string Summary { get; set; } = string.Empty;

    public string? RequestXml { get; set; }
    public string? ResponseText { get; set; }
    public string? Message { get; set; }

    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
