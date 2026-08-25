using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Identity;

/// <summary>
/// Persisted separately from ApplicationUser (which lives in Infrastructure/Identity)
/// so the Domain layer never needs a reference to the Identity packages.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
