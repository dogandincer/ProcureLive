using ProcureLive.Domain.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Companies;

/// <summary>Cari belgesi (vergi levhası, ISO sertifikası...). Geçerlilik tarihi yaklaşınca bir kez uyarı gider.</summary>
public class CompanyDocument : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public CompanyDocumentType DocumentType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public DateTime? ValidUntil { get; set; }

    public Guid? UploadedByUserId { get; set; }

    public DateTime? ExpiryWarnedAt { get; set; }
}
