using MediatR;
using ProcureLive.Application.Companies.Documents;

namespace ProcureLive.WebApi.HostedServices;

/// <summary>Cari belgelerinin son geçerlilik uyarısı: başlangıçta bir kez, sonra saatte bir (her belge için tek uyarı).</summary>
public sealed class DocumentExpiryHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentExpiryHostedService> _logger;

    public DocumentExpiryHostedService(IServiceScopeFactory scopeFactory, ILogger<DocumentExpiryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                int warned = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new WarnExpiringCompanyDocumentsCommand(), stoppingToken);
                if (warned > 0) _logger.LogInformation("{Count} belge için son geçerlilik uyarısı gönderildi.", warned);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "DocumentExpiryHostedService çalışırken hata oluştu.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }
}
