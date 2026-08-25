using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Domain.Entities.Rfi;

public class SupplierEvaluation : AuditableEntity
{
    public Guid RfiProcessId { get; set; }

    public RfiProcess RfiProcess { get; set; } = null!;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid EvaluatedByUserId { get; set; }

    public decimal TotalScore { get; set; }

    public string? Comments { get; set; }

    public DateTime EvaluatedAt { get; set; }
}
