using ProcureLive.Domain.Common;
using ProcureLive.Domain.ValueObjects;

namespace ProcureLive.Domain.Entities.Rfq;

public class RfqQuoteLineItem : BaseEntity
{
    public Guid RfqQuoteId { get; set; }

    public RfqQuote RfqQuote { get; set; } = null!;

    public Guid RfqLineItemId { get; set; }

    public RfqLineItem RfqLineItem { get; set; } = null!;

    public Money UnitPrice { get; set; } = null!;

    public int? LeadTimeDays { get; set; }

    public string? Notes { get; set; }
}
