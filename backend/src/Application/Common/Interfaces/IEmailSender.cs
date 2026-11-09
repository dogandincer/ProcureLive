using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Interfaces;

/// <summary>Giden e-posta. <paramref name="ActionUrl"/> verilirse şablonda "kaydı aç" düğmesi olarak gösterilir.</summary>
public sealed record EmailMessage(string To, string Subject, string Body, string? ActionUrl = null, string? ActionLabel = null);

/// <summary>
/// Uygulamanın e-posta gönderme noktası. Admin'in kaydettiği SMTP ayarı açıksa gerçekten gönderir, hata olursa istisna fırlatır;
/// ayar kapalı/eksikse yalnızca loglar (geliştirme ortamının eski davranışı).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);

    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
        => SendAsync(new EmailMessage(toEmail, subject, body), cancellationToken);
}

/// <summary>Verilen ayarla tek bir iletiyi SMTP üzerinden gönderir (kaydedilmemiş ayarla test göndermek için de kullanılır).</summary>
public interface IEmailTransport
{
    Task SendAsync(EmailSettingsSnapshot settings, EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>Kayıtlı e-posta ayarının çözülmüş (şifresi açılmış) hali; kısa süre önbelleklenir, kayıttan sonra <see cref="Invalidate"/> çağrılır.</summary>
public interface IEmailSettingsProvider
{
    Task<EmailSettingsSnapshot> GetAsync(CancellationToken cancellationToken);

    void Invalidate();
}

/// <summary>Uygulama içinde saklanan sırlar (SMTP şifresi) için simetrik koruma.</summary>
public interface ISecretProtector
{
    string Protect(string plainText);

    /// <summary>Anahtar değiştiyse veya veri bozuksa null döner.</summary>
    string? Unprotect(string protectedText);
}

public sealed record EmailSettingsSnapshot(
    bool Enabled,
    string Host,
    int Port,
    EmailSecurityMode Security,
    string? Username,
    string? Password,
    string FromAddress,
    string FromName,
    string? ReplyTo,
    IReadOnlySet<NotificationType> DisabledNotificationTypes)
{
    public static readonly EmailSettingsSnapshot Disabled = new(
        false, string.Empty, 587, EmailSecurityMode.StartTls, null, null, string.Empty, string.Empty, null, new HashSet<NotificationType>());

    /// <summary>Gerçek gönderim yapılabilir mi: açık ve sunucu/gönderen dolu.</summary>
    public bool IsActive => Enabled && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);

    public bool SendsType(NotificationType type) => !DisabledNotificationTypes.Contains(type);
}
