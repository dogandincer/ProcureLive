using Microsoft.AspNetCore.SignalR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Notifications;

namespace ProcureLive.WebApi.Hubs;

public sealed class SignalRNotificationRealtimeService : INotificationRealtimeService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationRealtimeService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyCompanyAsync(Guid companyId, NotificationDto notification, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(NotificationHub.CompanyGroupName(companyId))
            .SendAsync("NotificationReceived", notification, cancellationToken);
    }

    public Task NotifyInternalUsersAsync(NotificationDto notification, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(NotificationHub.InternalGroupName)
            .SendAsync("NotificationReceived", notification, cancellationToken);
    }

    public Task NotifyUserAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(NotificationHub.UserGroupName(userId))
            .SendAsync("NotificationReceived", notification, cancellationToken);
    }
}
