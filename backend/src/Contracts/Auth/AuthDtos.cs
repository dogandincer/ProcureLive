namespace ProcureLive.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    Guid? CompanyId);

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
