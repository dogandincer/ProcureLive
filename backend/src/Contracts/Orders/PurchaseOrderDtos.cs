namespace ProcureLive.Contracts.Orders;

public sealed record PurchaseOrderListItemDto(
    Guid Id,
    string OrderNumber,
    string SourceKind,
    Guid SourceId,
    string SourceTitle,
    Guid SupplierCompanyId,
    string SupplierCompanyName,
    string OwnerCompanyName,
    string CurrencyCode,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? ConfirmedAt,
    int LineCount);

public sealed record PurchaseOrderLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record PurchaseOrderDetailDto(
    Guid Id,
    string OrderNumber,
    string SourceKind,
    Guid SourceId,
    string SourceTitle,
    Guid OwnerCompanyId,
    string OwnerCompanyName,
    Guid SupplierCompanyId,
    string SupplierCompanyName,
    string CurrencyCode,
    decimal TotalAmount,
    string Status,
    string? Notes,
    DateTime? DeliveryDate,
    string? SupplierNote,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record UpdatePurchaseOrderRequest(string? Notes, DateTime? DeliveryDate);

public sealed record ConfirmPurchaseOrderRequest(string? Note);

public sealed record CancelPurchaseOrderRequest(string? Reason);
