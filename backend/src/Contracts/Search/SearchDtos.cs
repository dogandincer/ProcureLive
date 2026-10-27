namespace ProcureLive.Contracts.Search;

/// <summary>
/// KVKK: ShowContactInfo kapalıysa Email/Phone sunucuda maskelenir, Website hiç dönmez (ContactMasked=true).
/// Adres yalnızca kısa haliyle ("Beykoz/İstanbul") döner.
/// </summary>
public sealed record CompanySearchItemDto(
    Guid Id, string Name, string? TradeName, string? LogoUrl, string CompanyType, string? ShortAddress,
    IReadOnlyList<string> Sectors, decimal? AverageScore,
    string? Email, string? Phone, string? Website, bool ContactMasked, int OpenTenderCount);

public sealed record TenderSearchItemDto(
    Guid Id, string Title, string? ReferenceCode, string AuctionType, string Status,
    Guid OwnerCompanyId, string OwnerCompanyName, string? OwnerLogoUrl,
    DateTime StartAt, DateTime EndAt, int ItemCount, int ParticipantCount, bool IsJoined);

/// <summary>OpenTenderCount: bu ürünü isteyen, arayanın görebildiği açık ihale sayısı.</summary>
public sealed record ProductSearchItemDto(
    Guid Id, string Sku, string Name, string CategoryName, string UnitOfMeasure, string? ThumbnailUrl, int OpenTenderCount);

public sealed record SearchResultDto(
    IReadOnlyList<CompanySearchItemDto> Companies,
    IReadOnlyList<TenderSearchItemDto> Tenders,
    IReadOnlyList<ProductSearchItemDto> Products);
