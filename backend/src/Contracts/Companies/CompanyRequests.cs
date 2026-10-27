using ProcureLive.Contracts.Common;

namespace ProcureLive.Contracts.Companies;

public sealed record CreateCompanyRequest(
    string Name,
    string TaxNumber,
    string TaxOffice,
    string CompanyType,
    string? Website,
    Guid? WorkspaceId,
    string? TradeName,
    string? MersisNo,
    string? Phone,
    string? Email,
    int? FoundationYear,
    int? EmployeeCount,
    string? Description,
    string? Code = null,
    IReadOnlyList<Guid>? VisibleWorkspaceIds = null,
    IReadOnlyList<Guid>? SectorIds = null,
    string? UserEmail = null,
    string? UserPassword = null,
    string? UserFirstName = null,
    string? UserLastName = null);

public sealed record UpdateCompanyRequest(
    string Name,
    string TaxOffice,
    string Status,
    string? Website,
    string? TradeName,
    string? MersisNo,
    string? Phone,
    string? Email,
    int? FoundationYear,
    int? EmployeeCount,
    string? Description,
    string? Code = null);

public sealed record AssignWorkspaceRequest(Guid? WorkspaceId);

/// <summary>Carinin görünür olduğu işyerleri (birincil işyeri dahil edilmesine gerek yok).</summary>
public sealed record SetCompanyWorkspacesRequest(IReadOnlyList<Guid> WorkspaceIds);

public sealed record CreateCompanyUserRequest(string Email, string Password, string FirstName, string LastName);

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
