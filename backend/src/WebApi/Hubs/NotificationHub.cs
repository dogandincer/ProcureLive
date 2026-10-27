using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.WebApi.Hubs;

/// <summary>
/// Realtime notification channel. On connect, a client is placed into its own group automatically —
/// Suppliers into "company:{companyId}" (from the JWT claim, never client-supplied), Admin/Buyer into
/// "internal" — so a server-side push can never be misdirected to the wrong company.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        bool isInternalUser = Context.User?.IsInRole(Roles.Admin) == true || Context.User?.IsInRole(Roles.Buyer) == true;

        if (isInternalUser)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, InternalGroupName);

            string? userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value;
            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));
            }
        }
        else
        {
            string? companyIdClaim = Context.User?.FindFirst(ProcureLiveClaimTypes.CompanyId)?.Value;

            Guid companyId = Guid.Empty;
            bool parsed = companyIdClaim is not null && Guid.TryParse(companyIdClaim, out companyId);

            if (parsed)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, CompanyGroupName(companyId));
            }
        }

        await base.OnConnectedAsync();
    }

    public static string CompanyGroupName(Guid companyId) => $"company:{companyId}";

    public static string UserGroupName(Guid userId) => $"user:{userId}";

    public const string InternalGroupName = "internal";
}
