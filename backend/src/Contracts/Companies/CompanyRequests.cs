using ProcureLive.Contracts.Common;

namespace ProcureLive.Contracts.Companies;

public sealed record CreateCompanyRequest(
    string Name,
    string TaxNumber,
    string TaxOffice,
    string CompanyType,
    string? Website,
    Guid? WorkspaceId);

public sealed record UpdateCompanyRequest(
    string Name,
    string TaxOffice,
    string Status,
    string? Website);

public sealed record AssignWorkspaceRequest(Guid? WorkspaceId);

public sealed record AddContactPersonRequest(
    string FirstName,
    string LastName,
    string? Title,
    string Email,
    string? Phone,
    bool IsPrimary);

public sealed record AddAddressRequest(
    string AddressType,
    string Country,
    string City,
    string? District,
    string? PostalCode,
    string Line1,
    string? Line2);

public sealed record CompanyListFilter(
    string? SearchTerm,
    string? CompanyType,
    string? Status,
    int Page = 1,
    int PageSize = 20) : IPagedRequest;
