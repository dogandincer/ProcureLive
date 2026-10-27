namespace ProcureLive.Contracts.Companies;

public sealed record SectorDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int CompanyCount);


public sealed record CompanyDto(
    Guid Id,
    string? Code,
    string Name,
    string TaxNumber,
    string TaxOffice,
    string CompanyType,
    string Status,
    string? Website,
    Guid? WorkspaceId,
    string? WorkspaceName,
    decimal? AverageScore,
    int EvaluationCount,
    DateTime CreatedAt,
    IReadOnlyCollection<string> VisibleWorkspaceNames);

public sealed record CompanyDetailDto(
    Guid Id,
    string? Code,
    string Name,
    string? TradeName,
    string TaxNumber,
    string TaxOffice,
    string? MersisNo,
    string CompanyType,
    string Status,
    string? Website,
    string? Phone,
    string? Email,
    int? FoundationYear,
    int? EmployeeCount,
    string? Description,
    Guid? WorkspaceId,
    string? WorkspaceName,
    decimal? AverageScore,
    int EvaluationCount,
    DateTime CreatedAt,
    IReadOnlyCollection<ContactPersonDto> ContactPersons,
    IReadOnlyCollection<AddressDto> Addresses,
    IReadOnlyCollection<SectorDto> Sectors,
    IReadOnlyCollection<CompanyEvaluationDto> Evaluations,
    IReadOnlyCollection<Guid> VisibleWorkspaceIds);

public sealed record CompanyEvaluationDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    int QualityScore,
    int DeliveryScore,
    int CommunicationScore,
    int PriceScore,
    int ComplianceScore,
    decimal OverallScore,
    string? Comments,
    string? SourceType,
    DateTime EvaluatedAt);

public sealed record EvaluateCompanyRequest(
    int QualityScore,
    int DeliveryScore,
    int CommunicationScore,
    int PriceScore,
    int ComplianceScore,
    string? Comments,
    string? SourceType,
    Guid? SourceProcessId);

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
