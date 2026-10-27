namespace ProcureLive.Contracts.Auctions;

/// <summary>
/// Normalize master-detail ihale oluşturma payload'ı. 30 kalem × 40 cari kartezyeni GÖNDERİLMEZ;
/// üç düz koleksiyon gider (Master, Lines, VendorIds) ve davetler backend'de türetilir.
/// </summary>
public sealed record CreateTenderRequest(
    TenderMasterRequest Master,
    IReadOnlyCollection<TenderLineRequest> Lines,
    IReadOnlyCollection<Guid> VendorIds,
    IReadOnlyCollection<TenderAttachmentRequest> Attachments);

public sealed record TenderMasterRequest(
    string? ReferenceCode,
    string Title,
    string? Description,
    string AuctionType,
    Guid OwnerCompanyId,
    Guid CurrencyId,
    DateTime StartAt,
    DateTime EndAt,
    decimal? ReservePrice,
    int ExtensionTriggerMinutes,
    int? ExtensionDurationMinutes,
    int? MaxExtensionMinutes,
    string? PaymentTerm,
    string? DeliveryTerm,
    bool AllowAlternativeProducts,
    bool AllowPartialBids,
    bool IsListedForPlus = false);

public sealed record TenderLineRequest(
    Guid? ProductId,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? TargetPrice,
    DateTime? RequestedDeliveryDate,
    string? Specification,
    int SortOrder);

public sealed record TenderAttachmentRequest(
    string FileName,
    string Url,
    string? ContentType,
    long SizeBytes);
