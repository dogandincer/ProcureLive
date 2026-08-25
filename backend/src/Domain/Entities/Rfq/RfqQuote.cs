using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Rfq;

public class RfqQuote : AuditableEntity
{
    public Guid RfqProcessId { get; set; }

    public RfqProcess RfqProcess { get; set; } = null!;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid SubmittedByUserId { get; set; }

    public RfqQuoteStatus Status { get; set; } = RfqQuoteStatus.Draft;

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ValidUntil { get; set; }

    public string? Notes { get; set; }

    public ICollection<RfqQuoteLineItem> LineItems { get; set; } = new List<RfqQuoteLineItem>();
}
