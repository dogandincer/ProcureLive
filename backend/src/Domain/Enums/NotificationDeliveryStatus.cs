namespace ProcureLive.Domain.Enums;

public enum NotificationDeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,

    /// <summary>Gönderim bilinçli olarak yapılmadı (e-posta kapalı, tür devre dışı, alıcı yok).</summary>
    Skipped = 3
}
