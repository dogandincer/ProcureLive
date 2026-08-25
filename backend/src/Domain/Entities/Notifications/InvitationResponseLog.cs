using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Notifications;

/// <summary>
/// Records a supplier's Accept/Decline response to an Auction/RFI/RFQ invitation.
/// A Declined row with a Reason blocks that company from entering the related process.
/// </summary>
public class InvitationResponseLog : BaseEntity
{
    public InvitationSourceType SourceType { get; set; }

    public Guid SourceId { get; set; }

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid UserId { get; set; }

    public InvitationResponseType ResponseType { get; set; }

    public string? Reason { get; set; }

    public DateTime RespondedAt { get; set; }
}
