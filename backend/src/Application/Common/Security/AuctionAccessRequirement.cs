using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Security;

public sealed class AuctionAccessRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Admin/Buyer always pass. A Supplier user may only access an auction they were invited to
/// and have accepted/are participating in (or have already won/lost) — never a raw invite they declined.
/// </summary>
public sealed class AuctionAccessHandler : AuthorizationHandler<AuctionAccessRequirement, Auction>
{
    private static readonly InvitationStatus[] AllowedStatuses =
    {
        InvitationStatus.Accepted, InvitationStatus.Participating, InvitationStatus.Won, InvitationStatus.Lost
    };

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuctionAccessRequirement requirement,
        Auction resource)
    {
        if (context.User.IsInRole(Roles.Admin) || context.User.IsInRole(Roles.Buyer))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        string? companyIdClaim = context.User.FindFirstValue(ProcureLiveClaimTypes.CompanyId);

        Guid userCompanyId = Guid.Empty;
        bool parsed = Guid.TryParse(companyIdClaim, out userCompanyId);

        if (!parsed)
        {
            return Task.CompletedTask;
        }

        bool isInvitedAndAllowed = resource.Invitations.Any(i =>
            i.CompanyId == userCompanyId && AllowedStatuses.Contains(i.Status));

        if (isInvitedAndAllowed)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
