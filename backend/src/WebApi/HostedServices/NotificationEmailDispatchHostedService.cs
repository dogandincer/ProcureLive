using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.HostedServices;

/// <summary>
/// Polls for Pending Email NotificationChannelLog rows and dispatches them via IEmailSender.
/// Kept separate from the instant SignalR push (handled synchronously in NotificationCreator) because
/// email delivery is comparatively slow and benefits from being off the request path.
/// </summary>
public sealed class NotificationEmailDispatchHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationEmailDispatchHostedService> _logger;

    public NotificationEmailDispatchHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationEmailDispatchHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "NotificationEmailDispatchHostedService tick sırasında hata oluştu.");
            }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        IEmailSender emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        IDateTime dateTime = scope.ServiceProvider.GetRequiredService<IDateTime>();

        List<NotificationChannelLog> pending = await context.NotificationChannelLogs
            .Include(l => l.Notification)
            .Where(l => l.Channel == NotificationChannel.Email && l.Status == NotificationDeliveryStatus.Pending)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (NotificationChannelLog log in pending)
        {
            if (log.Notification.CompanyId is null)
            {
                log.Status = NotificationDeliveryStatus.Failed;
                log.ErrorMessage = "Bildirimin hedef Cari'si yok, e-posta gönderilemedi.";
                continue;
            }

            ContactPerson? primaryContact = await context.ContactPersons
                .Where(c => c.CompanyId == log.Notification.CompanyId && c.IsPrimary)
                .FirstOrDefaultAsync(cancellationToken);

            if (primaryContact is null)
            {
                log.Status = NotificationDeliveryStatus.Failed;
                log.ErrorMessage = "Cari için birincil iletişim kişisi/e-posta bulunamadı.";
                continue;
            }

            try
            {
                await emailSender.SendAsync(primaryContact.Email, log.Notification.Title, log.Notification.Body, cancellationToken);
                log.Status = NotificationDeliveryStatus.Sent;
                log.SentAt = dateTime.UtcNow;
            }
            catch (Exception ex)
            {
                log.Status = NotificationDeliveryStatus.Failed;
                log.ErrorMessage = ex.Message;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
