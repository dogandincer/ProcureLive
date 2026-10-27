namespace ProcureLive.Contracts.Invitations;

public sealed record CreateInviteLinkRequest(Guid CompanyId, string? Email, int? ValidDays);

public sealed record InviteLinkDto(
    Guid Id, string Token, string Path, Guid CompanyId, string CompanyName,
    string? Email, DateTime ExpiresAt, DateTime? UsedAt, bool IsExpired, DateTime CreatedAt);

/// <summary>Davet linki karşılama ekranı için (anonim) — geçersizse IsValid=false ve sebep kodu döner.</summary>
public sealed record InviteInfoDto(
    bool IsValid,
    string? InvalidReason,
    Guid? AuctionId,
    string? AuctionTitle,
    string? ReferenceCode,
    string? OwnerCompanyName,
    Guid? CompanyId,
    string? CompanyName,
    string? Email,
    DateTime? ExpiresAt,
    DateTime? AuctionEndAt,
    bool RegistrationUsed);

public sealed record RegisterWithInviteRequest(string Email, string Password, string FirstName, string LastName);

public sealed record ClaimInviteResultDto(Guid AuctionId);
