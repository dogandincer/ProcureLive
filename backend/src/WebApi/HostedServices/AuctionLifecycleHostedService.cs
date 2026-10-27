using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Participation;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.HostedServices;

/// <summary>
/// Polls for Scheduled auctions whose StartAt has passed (-> Live, broadcasts AuctionStarted), Live
/// auctions whose EndAt has passed (-> Ended, broadcasts AuctionEnded), and Scheduled auctions starting
/// within the reminder window (persisted "starting soon" notification to invitees). IHostedService is a
/// singleton, so a scoped DbContext/notification service is resolved per tick via IServiceScopeFactory.
/// </summary>
public sealed class AuctionLifecycleHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromMinutes(15);

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
        INotificationRealtimeService notificationRealtimeService = scope.ServiceProvider.GetRequiredService<INotificationRealtimeService>();

        DateTime now = dateTime.UtcNow;

        List<Auction> toStart = await context.Auctions
            .Where(a => a.Status == AuctionStatus.Scheduled && a.StartAt <= now)
            .ToListAsync(cancellationToken);

        // Otomatik geçişler de durum makinesinden geçer: tarih alanları + "Sistem" kayıtlı durum geçmişi.
        // Duraklatılmış (Paused) ihaleler burada hiç seçilmez; süreleri devam ettirilene kadar işlemez.
        foreach (Auction auction in toStart)
        {
            AuctionStatusMachine.Apply(context, auction, AuctionStatus.Live, now, userId: null, reason: null, automatic: true);
        }

        List<Auction> toEnd = await context.Auctions
            .Where(a => a.Status == AuctionStatus.Live && a.EndAt <= now)
            .ToListAsync(cancellationToken);

        foreach (Auction auction in toEnd)
        {
            AuctionStatusMachine.Apply(context, auction, AuctionStatus.Ended, now, userId: null, reason: null, automatic: true);
        }

        DateTime reminderThreshold = now.Add(ReminderWindow);

        List<Auction> toRemind = await context.Auctions
            .Where(a => a.Status == AuctionStatus.Scheduled
                && a.StartingReminderSentAt == null
                && a.StartAt > now
                && a.StartAt <= reminderThreshold)
            .ToListAsync(cancellationToken);

        foreach (Auction auction in toRemind)
        {
            auction.StartingReminderSentAt = now;
        }

        if (toStart.Count > 0 || toEnd.Count > 0 || toRemind.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (Auction auction in toStart)
        {
            await notificationService.AuctionStartedAsync(auction.Id, cancellationToken);
        }

        // Zamanı gelip canlıya geçen ihalelerde kabul edilmiş ön teklifler ilk teklif olarak yazılır.
        if (toStart.Count > 0)
        {
            PreOfferBidService preOffers = scope.ServiceProvider.GetRequiredService<PreOfferBidService>();
            foreach (Auction auction in toStart)
            {
                try
                {
                    await preOffers.ApplyAsync(auction.Id, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Ön teklifler işlenemedi (ihale {AuctionId}).", auction.Id);
                }
            }
        }

        foreach (Auction auction in toEnd)
        {
            await notificationService.AuctionEndedAsync(auction.Id, cancellationToken);
        }

        foreach (Auction auction in toRemind)
        {
            List<Guid> recipientCompanyIds = await context.AuctionInvitations
                .Where(i => i.AuctionId == auction.Id && i.Status != InvitationStatus.Declined)
                .Select(i => i.CompanyId)
                .ToListAsync(cancellationToken);

            int minutesLeft = Math.Max(1, (int)Math.Round((auction.StartAt - now).TotalMinutes));

            foreach (Guid companyId in recipientCompanyIds)
            {
                await NotificationCreator.CreateForCompanyAsync(
                    context, notificationRealtimeService, dateTime, companyId,
                    NotificationType.AuctionStarting,
                    "İhale Yakında Başlıyor",
                    $"\"{auction.Title}\" ihalesi yaklaşık {minutesLeft} dakika içinde başlıyor.",
                    nameof(Auction), auction.Id, cancellationToken);
            }
        }
    }
}
