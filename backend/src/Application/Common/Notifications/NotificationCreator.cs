using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Notifications;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Notifications;

/// <summary>
/// Shared helper used by invite/award handlers (Auction/RFI/RFQ) to record a Notification, push it
/// over SignalR immediately (InApp), and queue an Email channel log for the background dispatcher
/// when the target company has a primary contact with an email address.
/// </summary>
public static class NotificationCreator
{
    public static async Task CreateForCompanyAsync(
        IApplicationDbContext context,
        INotificationRealtimeService realtimeService,
        IDateTime dateTime,
        Guid companyId,
        NotificationType type,
        string title,
        string body,
        string? relatedEntityType,
        Guid? relatedEntityId,
        CancellationToken cancellationToken)
    {
        DateTime now = dateTime.UtcNow;

        var notification = new Notification
        {
            CompanyId = companyId,
            Type = type,
            Title = title,
            Body = body,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            IsRead = false
        };

        context.Notifications.Add(notification);

        context.NotificationChannelLogs.Add(new NotificationChannelLog
        {
            Notification = notification,
            Channel = NotificationChannel.InApp,
            Status = NotificationDeliveryStatus.Sent,
            SentAt = now
        });

        ContactPerson? primaryContact = await context.ContactPersons
            .Where(c => c.CompanyId == companyId && c.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        if (primaryContact is not null)
        {
            context.NotificationChannelLogs.Add(new NotificationChannelLog
            {
                Notification = notification,
                Channel = NotificationChannel.Email,
                Status = NotificationDeliveryStatus.Pending
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var dto = new NotificationDto(
            notification.Id, notification.Type.ToString(), notification.Title, notification.Body,
            notification.RelatedEntityType, notification.RelatedEntityId, notification.IsRead, null, now);

        await realtimeService.NotifyCompanyAsync(companyId, dto, cancellationToken);
    }

    /// <summary>
    /// İç kullanıcılara kişi başı bildirim (alıcılar <see cref="InternalNotificationAudience"/> ile seçilir). Her alıcıya ayrı satır
    /// yazılır: okundu bilgisi kişiye özeldir ve kapsam dışı kullanıcı bildirimi hiç görmez.
    /// </summary>
    public static async Task CreateForInternalUsersAsync(
        IApplicationDbContext context,
        INotificationRealtimeService realtimeService,
        IDateTime dateTime,
        IEnumerable<Guid> userIds,
        NotificationType type,
        string title,
        string body,
        string? relatedEntityType,
        Guid? relatedEntityId,
        CancellationToken cancellationToken)
    {
        foreach (Guid userId in userIds.Distinct())
        {
            await CreateForInternalUserAsync(context, realtimeService, dateTime, userId, type, title, body, relatedEntityType, relatedEntityId, cancellationToken);
        }
    }

    /// <summary>
    /// <paramref name="userId"/> verilirse bildirim yalnızca o iç kullanıcıya düşer (ör. ihalenin sorumlu satınalmacısı);
    /// null ise tüm iç kullanıcılara yayınlanır.
    /// </summary>
    public static async Task CreateForInternalUserAsync(
        IApplicationDbContext context,
        INotificationRealtimeService realtimeService,
        IDateTime dateTime,
        Guid? userId,
        NotificationType type,
        string title,
        string body,
        string? relatedEntityType,
        Guid? relatedEntityId,
        CancellationToken cancellationToken)
    {
        DateTime now = dateTime.UtcNow;

        var notification = new Notification
        {
            UserId = userId,
            CompanyId = null,
            Type = type,
            Title = title,
            Body = body,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            IsRead = false
        };

        context.Notifications.Add(notification);

        context.NotificationChannelLogs.Add(new NotificationChannelLog
        {
            Notification = notification,
            Channel = NotificationChannel.InApp,
            Status = NotificationDeliveryStatus.Sent,
            SentAt = now
        });

        await context.SaveChangesAsync(cancellationToken);

        var dto = new NotificationDto(
            notification.Id, notification.Type.ToString(), notification.Title, notification.Body,
            notification.RelatedEntityType, notification.RelatedEntityId, notification.IsRead, null, now);

        if (userId is Guid target)
            await realtimeService.NotifyUserAsync(target, dto, cancellationToken);
        else
            await realtimeService.NotifyInternalUsersAsync(dto, cancellationToken);
    }
}
