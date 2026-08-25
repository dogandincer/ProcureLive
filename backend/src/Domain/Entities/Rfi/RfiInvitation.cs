using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Rfi;

public class RfiInvitation : AuditableEntity
{
    public Guid RfiProcessId { get; set; }

    public RfiProcess RfiProcess { get; set; } = null!;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public InvitationStatus Status { get; set; } = InvitationStatus.Invited;

    public DateTime? SentAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}
