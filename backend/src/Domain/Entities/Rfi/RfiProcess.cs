using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Rfi;

public class RfiProcess : AuditableEntity
{
    public Guid RfiTemplateId { get; set; }

    public RfiTemplate RfiTemplate { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public Guid OwnerCompanyId { get; set; }

    public Company OwnerCompany { get; set; } = null!;

    /// <summary>Sürecin işyeri (sahip firmanın işyerlerinden). Satınalmacı kapsamı bu işyerine göre uygulanır.</summary>
    public Guid? WorkspaceId { get; set; }

    public ProcessStatus Status { get; set; } = ProcessStatus.Draft;

    public DateTime DueDate { get; set; }

    public ICollection<RfiInvitation> Invitations { get; set; } = new List<RfiInvitation>();

    public ICollection<RfiResponse> Responses { get; set; } = new List<RfiResponse>();

    public ICollection<SupplierEvaluation> Evaluations { get; set; } = new List<SupplierEvaluation>();
}
