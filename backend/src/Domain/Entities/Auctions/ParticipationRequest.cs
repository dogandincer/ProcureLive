using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Auctions;

/// <summary>
/// Davetsiz (ihale firmasının carisi olmayan) tedarikçinin ihaleye katılım talebi + kalem bazında ön teklifi.
/// Akış: Pending → (sorumlu satınalmacı cariyi onaylar) CompanyApproved → (ön teklifi inceleyip kabul eder) Accepted,
/// ya da herhangi bir adımda Rejected. Her adımda iki tarafa bildirim gider.
/// </summary>
public class ParticipationRequest : AuditableEntity
{
    public Guid AuctionId { get; set; }

    public Auction Auction { get; set; } = null!;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }

    public ParticipationRequestStatus Status { get; set; } = ParticipationRequestStatus.Pending;

    public string? Note { get; set; }

    public DateTime? CompanyApprovedAt { get; set; }

    public Guid? CompanyApprovedByUserId { get; set; }

    public DateTime? DecidedAt { get; set; }

    public Guid? DecidedByUserId { get; set; }

    public string? RejectReason { get; set; }

    public ICollection<ParticipationRequestLine> Lines { get; set; } = new List<ParticipationRequestLine>();
}

public class ParticipationRequestLine : BaseEntity
{
    public Guid ParticipationRequestId { get; set; }

    public ParticipationRequest ParticipationRequest { get; set; } = null!;

    public Guid AuctionItemId { get; set; }

    public AuctionItem AuctionItem { get; set; } = null!;

    /// <summary>Ön teklif birim fiyatı (ihalenin para biriminde).</summary>
    public decimal UnitPrice { get; set; }

    public string? Note { get; set; }
}
