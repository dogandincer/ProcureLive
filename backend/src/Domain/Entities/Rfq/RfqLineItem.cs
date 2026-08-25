using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Rfq;

public class RfqLineItem : BaseEntity
{
    public Guid RfqProcessId { get; set; }

    public RfqProcess RfqProcess { get; set; } = null!;

    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public UnitOfMeasure UnitOfMeasure { get; set; }

    public int SortOrder { get; set; }

    public ICollection<RfqQuoteLineItem> QuoteLineItems { get; set; } = new List<RfqQuoteLineItem>();
}
