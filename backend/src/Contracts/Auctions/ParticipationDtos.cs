namespace ProcureLive.Contracts.Auctions;

/// <summary>Katılım talebi + ön teklif. IsCompanyApproved: cari ihalenin işyerinde görünür (onaylı) mü.</summary>
public sealed record ParticipationRequestDto(
    Guid Id,
    Guid AuctionId,
    string AuctionTitle,
    Guid CompanyId,
    string CompanyName,
    string? CompanyCode,
    string TaxNumber,
    string CompanyStatus,
    bool IsCompanyApproved,
    string Status,
    string? Note,
    string CurrencyCode,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? CompanyApprovedAt,
    DateTime? DecidedAt,
    string? RejectReason,
    IReadOnlyList<ParticipationRequestLineDto> Lines);

public sealed record ParticipationRequestLineDto(
    Guid AuctionItemId,
    string ItemDescription,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal,
    string? Note);

public sealed record ParticipationLineInput(Guid AuctionItemId, decimal UnitPrice, string? Note);

public sealed record CreateParticipationRequestRequest(string? Note, IReadOnlyList<ParticipationLineInput> Lines);

public sealed record RejectParticipationRequestRequest(string? Reason);
