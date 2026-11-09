using ProcureLive.Application.Erp;

namespace ProcureLive.WebApi.HostedServices;

/// <summary>ERP (UyumERP) eşitleme kuyruğunu periyodik işler; premium anahtar kapalıysa dağıtıcı hiçbir şey yapmaz.</summary>
public sealed class ErpSyncHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErpSyncHostedService> _logger;

    public ErpSyncHostedService(IServiceScopeFactory scopeFactory, ILogger<ErpSyncHostedService> logger)
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
                await scope.ServiceProvider.GetRequiredService<ErpSyncDispatcher>().RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "ErpSyncHostedService tick sırasında hata oluştu.");
            }
        }
    }
}
