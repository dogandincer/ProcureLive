using ProcureLive.Contracts.Notifications;

namespace ProcureLive.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the SignalR NotificationHub. Every connected client joins a group for their own
/// CompanyId (Suppliers) and/or "internal" (Admin/Buyer) on connect; pushes are addressed to those groups.
/// </summary>
public interface INotificationRealtimeService
{
    Task NotifyCompanyAsync(Guid companyId, NotificationDto notification, CancellationToken cancellationToken);

    Task NotifyInternalUsersAsync(NotificationDto notification, CancellationToken cancellationToken);
}
