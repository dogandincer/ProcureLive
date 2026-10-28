namespace ProcureLive.Contracts.Auctions;

public sealed record UpdateAuctionDraftRequest(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    string? PaymentTerm,
    string? DeliveryTerm,
    decimal? ReservePrice,
    bool AllowPartialBids,
    bool AllowAlternativeProducts,
    bool IsListedForPlus,
    int ExtensionRuleMinutes,
    int? ExtensionDurationMinutes,
    int? MaxExtensionMinutes);

public sealed record UpdateAuctionItemRequest(
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? TargetPrice,
    DateTime? RequestedDeliveryDate,
    string? Specification);

public sealed record AddAuctionAttachmentRequest(string FileName, string Url, string? ContentType, long SizeBytes);
