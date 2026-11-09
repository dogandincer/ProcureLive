using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Common;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Notifications.Email;

/// <summary>
/// Bildirim e-postası kuyruğunu işler (arka plan servisinden periyodik çağrılır). İki aşama:
/// 1) Alıcısı henüz çözülmemiş kuyruk satırları genişletilir: cari bildirimi → carinin aktif kullanıcıları (kullanıcısı yoksa
///    birincil iletişim kişisi), kişiye özel iç bildirim → o kullanıcı. E-posta bildirimini kapatan kullanıcı atlanır.
///    E-posta kapalıysa veya tür Admin tarafından devre dışı bırakıldıysa satır "Skipped" olur — sonradan açmak eski bildirimleri göndermez.
/// 2) Alıcısı belli, zamanı gelmiş satırlar gönderilir; hata olursa 1 ve 5 dakika sonra yeniden denenir, 3. hatada "Failed".
/// </summary>
public sealed class NotificationEmailDispatcher
{
    public const int MaxAttempts = 3;
    private const int BatchSize = 50;

    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identity;
    private readonly IEmailSettingsProvider _settingsProvider;
    private readonly IEmailSender _sender;
    private readonly IAppUrlProvider _urls;
    private readonly IDateTime _dateTime;
    private readonly ILogger<NotificationEmailDispatcher> _logger;

    public NotificationEmailDispatcher(
        IApplicationDbContext context,
        IIdentityService identity,
        IEmailSettingsProvider settingsProvider,
        IEmailSender sender,
        IAppUrlProvider urls,
        IDateTime dateTime,
        ILogger<NotificationEmailDispatcher> logger)
    {
        _context = context;
        _identity = identity;
        _settingsProvider = settingsProvider;
        _sender = sender;
        _urls = urls;
        _dateTime = dateTime;
        _logger = logger;
    }

    /// <summary>Bu turda gönderilen e-posta sayısını döner.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        EmailSettingsSnapshot settings = await _settingsProvider.GetAsync(cancellationToken);
        await ExpandQueuedAsync(settings, cancellationToken);
        return await SendDueAsync(settings, cancellationToken);
    }

    private async Task ExpandQueuedAsync(EmailSettingsSnapshot settings, CancellationToken cancellationToken)
    {
        List<NotificationChannelLog> queued = await _context.NotificationChannelLogs
            .Include(l => l.Notification)
            .Where(l => l.Channel == NotificationChannel.Email && l.Status == NotificationDeliveryStatus.Pending && l.Recipient == null)
            .OrderBy(l => l.Notification.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (queued.Count == 0) return;

        foreach (NotificationChannelLog log in queued)
        {
            if (!settings.IsActive)
            {
                Skip(log, "E-posta gönderimi kapalı.");
                continue;
            }

            if (!settings.SendsType(log.Notification.Type))
            {
                Skip(log, "Bu bildirim türü için e-posta devre dışı.");
                continue;
            }

            IReadOnlyList<string> recipients = await ResolveRecipientsAsync(log.Notification, cancellationToken);
            if (recipients.Count == 0)
            {
                Skip(log, "E-posta almak isteyen alıcı bulunamadı.");
                continue;
            }

            log.Recipient = recipients[0];
            foreach (string recipient in recipients.Skip(1))
            {
                _context.NotificationChannelLogs.Add(new NotificationChannelLog
                {
                    NotificationId = log.NotificationId,
                    Channel = NotificationChannel.Email,
                    Status = NotificationDeliveryStatus.Pending,
                    Recipient = recipient
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> SendDueAsync(EmailSettingsSnapshot settings, CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;
        List<NotificationChannelLog> due = await _context.NotificationChannelLogs
            .Include(l => l.Notification)
            .Where(l => l.Channel == NotificationChannel.Email && l.Status == NotificationDeliveryStatus.Pending
                        && l.Recipient != null && (l.NextAttemptAt == null || l.NextAttemptAt <= now))
            .OrderBy(l => l.Notification.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (due.Count == 0) return 0;

        int sent = 0;
        foreach (NotificationChannelLog log in due)
        {
            if (!settings.IsActive)
            {
                // Gönderim beklerken kapatıldı: kuyrukta biriktirmek yerine atla (Admin "yeniden dene" diyemez, bilerek kapattı).
                Skip(log, "E-posta gönderimi kapalı.");
                continue;
            }

            Notification n = log.Notification;
            bool internalRecipient = n.CompanyId is null;
            string? path = NotificationLinks.For(n.Type, n.RelatedEntityType, n.RelatedEntityId, internalRecipient);
            var message = new EmailMessage(
                log.Recipient!, n.Title, n.Body,
                path is null ? null : _urls.FrontendBaseUrl + path,
                path is null ? null : "Kaydı aç");

            log.AttemptCount++;
            try
            {
                await _sender.SendAsync(message, cancellationToken);
                log.Status = NotificationDeliveryStatus.Sent;
                log.SentAt = _dateTime.UtcNow;
                log.ErrorMessage = null;
                log.NextAttemptAt = null;
                sent++;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                log.ErrorMessage = EmailErrors.Describe(ex);
                if (log.AttemptCount >= MaxAttempts)
                {
                    log.Status = NotificationDeliveryStatus.Failed;
                    log.NextAttemptAt = null;
                }
                else
                {
                    log.NextAttemptAt = _dateTime.UtcNow.AddMinutes(log.AttemptCount == 1 ? 1 : 5);
                }

                _logger.LogWarning(ex, "Bildirim e-postası gönderilemedi ({Attempt}/{Max}): {Recipient}", log.AttemptCount, MaxAttempts, log.Recipient);
            }

            // Her iletiden sonra kaydet: uzun bir turda süreç kapanırsa gönderilenler tekrar gönderilmesin.
            await _context.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    private async Task<IReadOnlyList<string>> ResolveRecipientsAsync(Notification notification, CancellationToken cancellationToken)
    {
        var emails = new List<string>();

        if (notification.UserId is Guid userId)
        {
            ApplicationUserInfo? user = await _identity.FindByIdAsync(userId, cancellationToken);
            if (user is { IsActive: true, EmailNotificationsEnabled: true }) emails.Add(user.Email);
        }
        else if (notification.CompanyId is Guid companyId)
        {
            PagedResult<ApplicationUserInfo> users = await _identity.ListUsersAsync(null, companyId, 1, 100, null, cancellationToken);
            if (users.Items.Count > 0)
            {
                emails.AddRange(users.Items.Where(u => u is { IsActive: true, EmailNotificationsEnabled: true }).Select(u => u.Email));
            }
            else
            {
                // Giriş hesabı olmayan cari (ör. henüz kullanıcı açılmamış): bildirim birincil iletişim kişisine gider.
                ContactPerson? contact = await _context.ContactPersons.AsNoTracking()
                    .Where(c => c.CompanyId == companyId && c.Email != "")
                    .OrderByDescending(c => c.IsPrimary)
                    .FirstOrDefaultAsync(cancellationToken);
                if (contact is not null) emails.Add(contact.Email);
            }
        }

        return emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .DistinctBy(e => e.ToLowerInvariant())
            .ToList();
    }

    private static void Skip(NotificationChannelLog log, string reason)
    {
        log.Status = NotificationDeliveryStatus.Skipped;
        log.ErrorMessage = reason;
        log.NextAttemptAt = null;
    }
}
