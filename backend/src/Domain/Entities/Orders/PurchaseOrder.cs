using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Finance;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Orders;

/// <summary>Sonuçlanan ihale ya da RFQ'dan kazanan tedarikçiye açılan sipariş (taslak → gönderildi → onaylandı).</summary>
public class PurchaseOrder : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public ProcessKind SourceKind { get; set; }

    public Guid SourceId { get; set; }

    public string SourceTitle { get; set; } = string.Empty;

    public Guid OwnerCompanyId { get; set; }

    public Company OwnerCompany { get; set; } = null!;

    public Guid? WorkspaceId { get; set; }

    public Guid SupplierCompanyId { get; set; }

    public Company SupplierCompany { get; set; } = null!;

    public Guid CurrencyId { get; set; }

    public Currency Currency { get; set; } = null!;

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? SupplierNote { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid? SourceItemId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public UnitOfMeasure UnitOfMeasure { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public int SortOrder { get; set; }
}
