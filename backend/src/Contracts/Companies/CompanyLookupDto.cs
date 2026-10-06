namespace ProcureLive.Contracts.Companies;

/// <summary>
/// TCKN/VKN sorgusu sonucu — cari tanım alanlarını otomatik doldurmak için öneri verisi.
/// (Simülasyon: numara format/algoritma doğrulaması gerçek, dönen bilgiler mock.)
/// </summary>
public sealed record CompanyLookupResultDto(
    string Identifier,
    string IdentifierType,
    bool IsValid,
    string? Name,
    string? TradeName,
    string? TaxOffice,
    string? MersisNo,
    string? City,
    string? District,
    string? AddressLine,
    string? Phone,
    string? Email,
    int? FoundationYear,
    string? Message);
