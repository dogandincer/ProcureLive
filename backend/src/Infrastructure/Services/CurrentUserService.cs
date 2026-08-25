using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            string? value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            Guid userId = Guid.Empty;
            bool parsed = value is not null && Guid.TryParse(value, out userId);

            return parsed ? userId : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public string? UserName => Principal?.Identity?.Name ?? Email;

    public Guid? CompanyId
    {
        get
        {
            string? value = Principal?.FindFirstValue(ProcureLiveClaimTypes.CompanyId);

            Guid companyId = Guid.Empty;
            bool parsed = value is not null && Guid.TryParse(value, out companyId);

            return parsed ? companyId : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
