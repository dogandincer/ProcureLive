namespace ProcureLive.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    Guid? CompanyId);

/// <summary>Davetsiz tedarikçi kendi kaydı (anonim).</summary>
public sealed record RegisterSupplierRequest(
    string CompanyName,
    string TaxNumber,
    string TaxOffice,
    string? Phone,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    bool AcceptTerms);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record RevokeTokenRequest(string RefreshToken);

public sealed record AuthResultDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    Guid? CompanyId,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken);

public sealed record CurrentUserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    Guid? CompanyId);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
