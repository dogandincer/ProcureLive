using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Notifications;

public class Notification : AuditableEntity
{
    public Guid? UserId { get; set; }

    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? RelatedEntityType { get; set; }

    public Guid? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public ICollection<NotificationChannelLog> ChannelLogs { get; set; } = new List<NotificationChannelLog>();
}
