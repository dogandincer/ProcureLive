using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Collaboration;

/// <summary>
/// Süreç (ihale/RFI/RFQ) üzerinde tedarikçi sorusu veya satınalmacı duyurusu. Soru, cevap herkese açık yapılırsa
/// tüm katılımcılara görünür (soran firma adı gizli kalır); duyuru her zaman herkese açıktır.
/// </summary>
public class ProcessQuestion : AuditableEntity
{
    public ProcessKind ProcessKind { get; set; }

    public Guid ProcessId { get; set; }

    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public Guid? AskedByUserId { get; set; }

    public string Question { get; set; } = string.Empty;

    public string? Answer { get; set; }

    public Guid? AnsweredByUserId { get; set; }

    public DateTime? AnsweredAt { get; set; }

    public bool IsPublic { get; set; }

    public bool IsAnnouncement { get; set; }
}
