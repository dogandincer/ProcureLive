using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.HostedServices;

/// <summary>
/// Polls for Scheduled auctions whose StartAt has passed (-> Live, broadcasts AuctionStarted) and
/// Live auctions whose EndAt has passed (-> Ended, broadcasts AuctionEnded). IHostedService is a
/// singleton, so a scoped DbContext/notification service is resolved per tick via IServiceScopeFactory.
/// </summary>
public sealed class AuctionLifecycleHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionLifecycleHostedService> _logger;

    public AuctionLifecycleHostedService(IServiceScopeFactory scopeFactory, ILogger<AuctionLifecycleHostedService> logger)
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
                _logger.LogError(ex, "AuctionLifecycleHostedService tick sırasında hata oluştu.");
            }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        IDateTime dateTime = scope.ServiceProvider.GetRequiredService<IDateTime>();
        IAuctionNotificationService notificationService = scope.ServiceProvider.GetRequiredService<IAuctionNotificationService>();

        DateTime now = dateTime.UtcNow;

        List<Auction> toStart = await context.Auctions
            .Where(a => a.Status == AuctionStatus.Scheduled && a.StartAt <= now)
            .ToListAsync(cancellationToken);

        foreach (Auction auction in toStart)
        {
            auction.Status = AuctionStatus.Live;
            auction.ActualStartAt = now;
        }

        List<Auction> toEnd = await context.Auctions
            .Where(a => a.Status == AuctionStatus.Live && a.EndAt <= now)
            .ToListAsync(cancellationToken);

        foreach (Auction auction in toEnd)
        {
            auction.Status = AuctionStatus.Ended;
            auction.ActualEndAt = now;
        }

        if (toStart.Count > 0 || toEnd.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (Auction auction in toStart)
        {
            await notificationService.AuctionStartedAsync(auction.Id, cancellationToken);
        }

        foreach (Auction auction in toEnd)
        {
            await notificationService.AuctionEndedAsync(auction.Id, cancellationToken);
        }
    }
}
