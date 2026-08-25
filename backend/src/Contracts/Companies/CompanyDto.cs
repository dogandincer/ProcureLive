namespace ProcureLive.Contracts.Companies;

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string TaxNumber,
    string TaxOffice,
    string CompanyType,
    string Status,
    string? Website,
    DateTime CreatedAt);

public sealed record CompanyDetailDto(
    Guid Id,
    string Name,
    string TaxNumber,
    string TaxOffice,
    string CompanyType,
    string Status,
    string? Website,
    DateTime CreatedAt,
    IReadOnlyCollection<ContactPersonDto> ContactPersons,
    IReadOnlyCollection<AddressDto> Addresses);

public sealed record ContactPersonDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Title,
    string Email,
    string? Phone,
    bool IsPrimary);

public sealed record AddressDto(
    Guid Id,
    string AddressType,
    string Country,
    string City,
    string? District,
    string? PostalCode,
    string Line1,
    string? Line2);
