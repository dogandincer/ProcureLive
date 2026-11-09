using ProcureLive.Domain.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Notifications;

/// <summary>
/// Sistem geneli giden e-posta (SMTP) ayarı — tabloda tek satır tutulur, yalnızca Admin değiştirir.
/// Şifre düz metin saklanmaz: <see cref="SmtpPasswordProtected"/> Data Protection ile şifrelenmiş haldedir ve API'den hiç dönmez.
/// </summary>
public class EmailSettings : AuditableEntity
{
    /// <summary>Kapalıyken bildirim e-postaları kuyruğa alınmaz, doğrudan gönderimler (şifre sıfırlama) yalnızca loglanır.</summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public EmailSecurityMode Security { get; set; } = EmailSecurityMode.StartTls;
    public string? Username { get; set; }
    public string? SmtpPasswordProtected { get; set; }

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? ReplyTo { get; set; }

    /// <summary>E-postası gönderilmeyecek bildirim türleri (<see cref="NotificationType"/> değerleri); boşsa hepsi gönderilir.</summary>
    public int[] DisabledNotificationTypes { get; set; } = [];
}
