using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Common.Security;

public sealed class CompanyAccessRequirement : IAuthorizationRequirement
{
}

/// <summary>Admin/Buyer (internal users) always pass; a Supplier user may only access their own Company.</summary>
public sealed class CompanyAccessHandler : AuthorizationHandler<CompanyAccessRequirement, Company>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompanyAccessRequirement requirement,
        Company resource)
    {
        if (context.User.IsInRole(Roles.Admin) || context.User.IsInRole(Roles.Buyer))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        string? companyIdClaim = context.User.FindFirstValue(ProcureLiveClaimTypes.CompanyId);

        Guid userCompanyId;
        bool parsed = Guid.TryParse(companyIdClaim, out userCompanyId);

        if (parsed && userCompanyId == resource.Id)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
