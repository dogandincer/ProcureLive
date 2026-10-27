namespace ProcureLive.Application.Common.Models;

/// <summary>Identity-agnostic view of a user, returned by IIdentityService so Application never touches IdentityUser directly.</summary>
public sealed record ApplicationUserInfo(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid? CompanyId,
    bool IsActive,
    DateTime? LastLoginAt,
    IReadOnlyCollection<string> Roles,
    DateTime? LockedOutAt = null);

public sealed record IdentityOperationResult(bool Succeeded, Guid? UserId, IReadOnlyCollection<string> Errors)
{
    public static IdentityOperationResult Success(Guid userId) => new(true, userId, Array.Empty<string>());

    public static IdentityOperationResult Failure(IReadOnlyCollection<string> errors) => new(false, null, errors);
}
