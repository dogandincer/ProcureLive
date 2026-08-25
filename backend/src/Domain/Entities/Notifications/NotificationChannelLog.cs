using ProcureLive.Domain.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Notifications;

public class NotificationChannelLog : BaseEntity
{
    public Guid NotificationId { get; set; }

    public Notification Notification { get; set; } = null!;

    public NotificationChannel Channel { get; set; }

    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;

    public DateTime? SentAt { get; set; }

    public string? ErrorMessage { get; set; }
}
