using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Finance;

namespace ProcureLive.WebApi.HostedServices;

/// <summary>
/// Refreshes exchange rates from an external provider (TCMB) once at startup, then every hour.
/// IHostedService is a singleton, so a scoped DbContext/provider is resolved per tick via
/// IServiceScopeFactory. A failed fetch (network down, TCMB unreachable) is logged and skipped -
/// it never crashes the app or blocks the next scheduled attempt.
/// </summary>
public sealed class ExchangeRateSyncHostedService : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExchangeRateSyncHostedService> _logger;

    public ExchangeRateSyncHostedService(IServiceScopeFactory scopeFactory, ILogger<ExchangeRateSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "ExchangeRateSyncHostedService: kur senkronizasyonu başarısız oldu, bir sonraki denemede tekrar denenecek.");
            }

            try
            {
                await Task.Delay(SyncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        IExchangeRateProvider provider = scope.ServiceProvider.GetRequiredService<IExchangeRateProvider>();
        IDateTime dateTime = scope.ServiceProvider.GetRequiredService<IDateTime>();

        IReadOnlyCollection<ExternalExchangeRate> externalRates = await provider.GetLatestRatesAsync(cancellationToken);

        if (externalRates.Count == 0)
        {
            _logger.LogWarning("ExchangeRateSyncHostedService: {Source} kaynağından hiçbir kur alınamadı.", provider.SourceName);
            return;
        }

        List<Currency> currencies = await context.Currencies
            .Where(c => !c.IsBaseCurrency)
            .ToListAsync(cancellationToken);

        if (currencies.Count == 0)
        {
            return;
        }

        Dictionary<string, decimal> rateByCode = externalRates
            .GroupBy(r => r.CurrencyCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().RateToBase, StringComparer.OrdinalIgnoreCase);

        DateOnly today = DateOnly.FromDateTime(dateTime.UtcNow);
        int inserted = 0;
        int updated = 0;

        foreach (Currency currency in currencies)
        {
            decimal rateToBase;
            bool found = rateByCode.TryGetValue(currency.Code, out rateToBase);

            if (!found)
            {
                continue;
            }

            ExchangeRate? existing = await context.ExchangeRates
                .FirstOrDefaultAsync(r => r.CurrencyId == currency.Id && r.RateDate == today, cancellationToken);

            if (existing is not null)
            {
                existing.RateToBase = rateToBase;
                existing.Source = provider.SourceName;
                updated++;
            }
            else
            {
                context.ExchangeRates.Add(new ExchangeRate
                {
                    CurrencyId = currency.Id,
                    RateDate = today,
                    RateToBase = rateToBase,
                    Source = provider.SourceName
                });
                inserted++;
            }
        }

        if (inserted > 0 || updated > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "ExchangeRateSyncHostedService: {Source} kaynağından {Inserted} yeni, {Updated} güncellenmiş kur kaydedildi.",
            provider.SourceName, inserted, updated);
    }
}
