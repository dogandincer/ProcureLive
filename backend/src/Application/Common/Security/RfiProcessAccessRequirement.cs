using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Security;

public sealed class RfiProcessAccessRequirement : IAuthorizationRequirement
{
}

/// <summary>Admin/Buyer always pass. A Supplier may view an RFI process only if their company was invited to it.</summary>
public sealed class RfiProcessAccessHandler : AuthorizationHandler<RfiProcessAccessRequirement, RfiProcess>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RfiProcessAccessRequirement requirement,
        RfiProcess resource)
    {
        if (context.User.IsInRole(Roles.Admin) || context.User.IsInRole(Roles.Buyer))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        string? companyIdClaim = context.User.FindFirstValue(ProcureLiveClaimTypes.CompanyId);

        Guid userCompanyId = Guid.Empty;
        bool parsed = Guid.TryParse(companyIdClaim, out userCompanyId);

        bool isInvited = parsed && resource.Invitations.Any(i =>
            i.CompanyId == userCompanyId && i.Status != InvitationStatus.Declined);

        if (isInvited)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
