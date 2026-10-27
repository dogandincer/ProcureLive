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
    DateTime? LastLoginAt,
    DateTime? LockedOutAt = null);

/// <summary>Satınalmacıya Admin'in verdiği işyeri yetkileri (firma yetkisi işyerinin firmasından türer).</summary>
public sealed record UserWorkspaceAccessDto(Guid UserId, IReadOnlyList<Guid> WorkspaceIds);

public sealed record SetUserWorkspaceAccessRequest(IReadOnlyList<Guid> WorkspaceIds);

public sealed record UpdateUserRequest(string FirstName, string LastName, Guid? CompanyId);

public sealed record AssignRolesRequest(IReadOnlyCollection<string> Roles);

public sealed record UserListFilter(
    string? SearchTerm,
    Guid? CompanyId,
    int Page = 1,
    int PageSize = 20) : IPagedRequest;
