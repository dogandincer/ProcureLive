using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Auctions;

/// <summary>Combined participant + invitation record for a supplier company on an auction.</summary>
public class AuctionInvitation : AuditableEntity
{
    public Guid AuctionId { get; set; }

    public Auction Auction { get; set; } = null!;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid InvitedByUserId { get; set; }

    public InvitationStatus Status { get; set; } = InvitationStatus.Invited;

    public InvitationOrigin Origin { get; set; } = InvitationOrigin.Direct;

    public DateTime InvitedAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}
