using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Finance;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Rfq;

public class RfqProcess : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public Guid OwnerCompanyId { get; set; }

    public Company OwnerCompany { get; set; } = null!;

    public Guid CurrencyId { get; set; }

    public Currency Currency { get; set; } = null!;

    public ProcessStatus Status { get; set; } = ProcessStatus.Draft;

    public DateTime DueDate { get; set; }

    public ICollection<RfqLineItem> LineItems { get; set; } = new List<RfqLineItem>();

    public ICollection<RfqInvitation> Invitations { get; set; } = new List<RfqInvitation>();

    public ICollection<RfqQuote> Quotes { get; set; } = new List<RfqQuote>();

    public Guid? WinningQuoteId { get; set; }

    public DateTime? AwardedAt { get; set; }

    public string? AwardNotes { get; set; }
}
