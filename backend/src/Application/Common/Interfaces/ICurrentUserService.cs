using System.Security.Claims;

namespace ProcureLive.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    string? UserName { get; }

    Guid? CompanyId { get; }

    IReadOnlyCollection<string> Roles { get; }

    ClaimsPrincipal? Principal { get; }

    string? IpAddress { get; }

    bool IsInRole(string role);
}
