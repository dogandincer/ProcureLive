namespace ProcureLive.Contracts.Firms;

public sealed record FirmDto(
    Guid Id,
    string Code,
    string Name,
    string? TradeName,
    string? ShortName,
    string? Phone,
    string? Phone2,
    string? MobilePhone,
    string? Fax,
    string? Email,
    string? Email2,
    string? Website,
    string? TaxOffice,
    string? TaxNumber,
    string? BillingCountry,
    string? BillingCity,
    string? BillingDistrict,
    string? BillingPostalCode,
    string? BillingAddressLine,
    string? LogoUrl,
    bool IsDefault,
    string? AppTitleSuffix,
    int WorkspaceCount,
    bool RequirePublishApproval = false);

/// <summary>Firma kaydet (oluştur/güncelle). Yalnızca Kod ve Ad zorunludur.</summary>
public sealed record SaveFirmRequest(
    string Code,
    string Name,
    string? TradeName,
    string? ShortName,
    string? Phone,
    string? Phone2,
    string? MobilePhone,
    string? Fax,
    string? Email,
    string? Email2,
    string? Website,
    string? TaxOffice,
    string? TaxNumber,
    string? BillingCountry,
    string? BillingCity,
    string? BillingDistrict,
    string? BillingPostalCode,
    string? BillingAddressLine,
    bool IsDefault,
    string? AppTitleSuffix,
    bool RequirePublishApproval = true);

/// <summary>Giriş ekranı ve sekme markası (anonim). AppName = "{Kısa Ad} {Ek}" veya temel ad.</summary>
public sealed record BrandingDto(string AppName, string? ShortName, string Suffix, string? LogoUrl, bool IsCustom);
