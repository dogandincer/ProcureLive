using ProcureLive.Application.Notifications.Email;

namespace ProcureLive.WebApi.HostedServices;

/// <summary>
/// Periodically drains the notification e-mail queue via <see cref="NotificationEmailDispatcher"/> (recipient resolution,
/// SMTP delivery, retries). Kept separate from the instant SignalR push (handled synchronously in NotificationCreator) because
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
                using IServiceScope scope = _scopeFactory.CreateScope();
                NotificationEmailDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<NotificationEmailDispatcher>();
                await dispatcher.RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "NotificationEmailDispatchHostedService tick sırasında hata oluştu.");
            }
        }
    }
}
