namespace ProcureLive.Contracts.Notifications;

/// <summary>Kayıtlı SMTP ayarı. Şifre hiçbir zaman dönmez, yalnızca kayıtlı olup olmadığı (<see cref="HasPassword"/>).</summary>
public sealed record EmailSettingsDto(
    bool Enabled,
    string Host,
    int Port,
    string Security,
    string? Username,
    bool HasPassword,
    string FromAddress,
    string FromName,
    string? ReplyTo,
    IReadOnlyCollection<string> DisabledNotificationTypes,
    IReadOnlyCollection<string> NotificationTypes,
    DateTime? UpdatedAt,
    string? UpdatedBy);

/// <summary>
/// Ayar kaydı ve test gönderimi aynı gövdeyi kullanır. <see cref="Password"/> boşsa kayıtlı şifre korunur;
/// <see cref="ClearPassword"/> true ise kayıtlı şifre silinir (kimlik doğrulamasız sunucular için).
/// </summary>
public sealed record EmailSettingsRequest(
    bool Enabled,
    string Host,
    int Port,
    string Security,
    string? Username,
    string? Password,
    bool ClearPassword,
    string FromAddress,
    string FromName,
    string? ReplyTo,
    IReadOnlyCollection<string>? DisabledNotificationTypes);

public sealed record SendTestEmailRequest(string To, EmailSettingsRequest Settings);

public sealed record TestEmailResultDto(bool Succeeded, string? Error);

public sealed record EmailLogDto(
    Guid Id,
    string? Recipient,
    string Subject,
    string NotificationType,
    string Status,
    int AttemptCount,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? SentAt);

public sealed record EmailLogSummaryDto(int Pending, int Sent, int Failed, int Skipped);
