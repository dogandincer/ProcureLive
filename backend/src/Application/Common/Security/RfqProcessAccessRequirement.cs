using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Security;

public sealed class RfqProcessAccessRequirement : IAuthorizationRequirement
{
}

/// <summary>Admin/Buyer always pass. A Supplier may view an RFQ process only if their company was invited to it.</summary>
public sealed class RfqProcessAccessHandler : AuthorizationHandler<RfqProcessAccessRequirement, RfqProcess>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RfqProcessAccessRequirement requirement,
        RfqProcess resource)
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
