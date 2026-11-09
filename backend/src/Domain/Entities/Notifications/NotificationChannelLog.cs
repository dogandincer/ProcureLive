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

    /// <summary>
    /// E-posta kanalında gerçek alıcı adresi. Bildirim oluşturulurken boş bir "kuyruk" satırı yazılır; dağıtıcı alıcıları
    /// (kullanıcı tercihine göre) çözer, bu satırı ilk alıcıya atar ve diğerleri için yeni satır açar.
    /// </summary>
    public string? Recipient { get; set; }

    public int AttemptCount { get; set; }

    /// <summary>Geçici hatadan sonra yeniden denenecek en erken an; null ise hemen.</summary>
    public DateTime? NextAttemptAt { get; set; }
}
