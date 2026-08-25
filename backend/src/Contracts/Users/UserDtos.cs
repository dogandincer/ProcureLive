using ProcureLive.Contracts.Common;

namespace ProcureLive.Contracts.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    Guid? CompanyId,
    bool IsActive,
    DateTime? LastLoginAt);

public sealed record UpdateUserRequest(string FirstName, string LastName, Guid? CompanyId);

public sealed record AssignRolesRequest(IReadOnlyCollection<string> Roles);

public sealed record UserListFilter(
    string? SearchTerm,
    Guid? CompanyId,
    int Page = 1,
    int PageSize = 20) : IPagedRequest;
