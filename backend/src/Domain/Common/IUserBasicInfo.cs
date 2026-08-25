namespace ProcureLive.Domain.Common;

/// <summary>
/// Domain-safe view of the current user. The real ApplicationUser (IdentityUser)
/// lives in Infrastructure so Identity package dependencies never leak into Domain.
/// </summary>
public interface IUserBasicInfo
{
    Guid Id { get; }

    string? Email { get; }

    Guid? CompanyId { get; }
}
