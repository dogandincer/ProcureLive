using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Status;

/// <summary>
/// Elle durum değişikliği: geçiş izni (<see cref="AuctionStatusMachine"/>) + duruma özgü doğrulama + kayıt +
/// bildirim. ChangeAuctionStatus ve (geriye dönük uyumlu) CancelAuction aynı yoldan geçer.
/// </summary>
public sealed class AuctionStatusService
{
    private static readonly TimeSpan MinRemaining = TimeSpan.FromMinutes(1);

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IAuctionNotificationService _auctionNotifications;
    private readonly INotificationRealtimeService _notificationRealtimeService;

    public AuctionStatusService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IAuctionNotificationService auctionNotifications,
        INotificationRealtimeService notificationRealtimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _auctionNotifications = auctionNotifications;
        _notificationRealtimeService = notificationRealtimeService;
    }

    public async Task ChangeAsync(
        Guid auctionId,
        AuctionStatus to,
        string? reason,
        DateTime? newStartAt,
        DateTime? newEndAt,
        CancellationToken cancellationToken)
    {
        Auction auction = await _context.Auctions
            .Include(a => a.Items)
            .Include(a => a.Invitations)
            .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), auctionId);

        AuctionStatus from = auction.Status;

        if (!AuctionStatusMachine.CanTransition(from, to))
        {
            throw new ConflictException(
                $"\"{AuctionStatusMachine.DisplayName(from)}\" durumundan \"{AuctionStatusMachine.DisplayName(to)}\" durumuna geçiş yapılamaz.",
                ErrorCodes.InvalidStatusTransition);
        }

        if (AuctionStatusMachine.RequiresReason(to) && string.IsNullOrWhiteSpace(reason))
        {
            throw new ConflictException("Bu durum değişikliği için gerekçe zorunludur.");
        }

        DateTime now = _dateTime.UtcNow;
        bool hasAwards = await _context.AuctionAwards.AnyAsync(a => a.AuctionId == auction.Id, cancellationToken);

        switch (to)
        {
            case AuctionStatus.Scheduled:
                EnsurePublishable(auction);
                DateTime start = newStartAt ?? auction.StartAt;
                if (start <= now)
                    throw new ConflictException("Başlangıç tarihi gelecekte olmalıdır.");
                if ((newEndAt ?? auction.EndAt) <= start)
                    throw new ConflictException("Bitiş tarihi başlangıçtan sonra olmalıdır.");
                break;

            case AuctionStatus.Live when from == AuctionStatus.Scheduled:
                EnsurePublishable(auction);
                EnsureEndsLater(newEndAt ?? auction.EndAt, now);
                break;

            case AuctionStatus.Live when from == AuctionStatus.Paused:
                EnsureEndsLater(newEndAt ?? auction.EndAt + (now - (auction.PausedAt ?? now)), now);
                break;

            case AuctionStatus.Live when from == AuctionStatus.Ended:
                if (hasAwards)
                    throw new ConflictException("Kazanan seçilmiş kalemleri olan ihale yeniden açılamaz.");
                if (newEndAt is null)
                    throw new ConflictException("İhaleyi yeniden açmak için yeni bir bitiş zamanı girin.");
                EnsureEndsLater(newEndAt.Value, now);
                break;

            case AuctionStatus.Cancelled when from == AuctionStatus.Ended:
                if (hasAwards)
                    throw new ConflictException("Kazanan seçilmiş kalemleri olan ihale iptal edilemez.");
                break;
        }

        AuctionStatusMachine.Apply(_context, auction, to, now, _currentUserService.UserId, reason, automatic: false, newStartAt, newEndAt);
        await _context.SaveChangesAsync(cancellationToken);

        await NotifyAsync(auction, from, to, reason, cancellationToken);
    }

    private static void EnsurePublishable(Auction auction)
    {
        if (auction.Items.Count == 0)
            throw new ConflictException("İhale yayınlanmadan önce en az bir kalem eklenmelidir.");
        if (auction.Invitations.Count == 0)
            throw new ConflictException("İhale yayınlanmadan önce en az bir Cari davet edilmelidir.");
    }

    private static void EnsureEndsLater(DateTime endAt, DateTime now)
    {
        if (endAt <= now + MinRemaining)
            throw new ConflictException("Bitiş zamanı en az 1 dakika ileride olmalıdır.");
    }

    private async Task NotifyAsync(Auction auction, AuctionStatus from, AuctionStatus to, string? reason, CancellationToken ct)
    {
        // Açık ihale sayfaları (admin + tedarikçi) bu tek olayla kendini yeniler; AuctionStarted/AuctionEnded
        // zamanlayıcının otomatik geçişlerine kalır (aynı değişiklik için akışa çift kayıt düşmesin).
        await _auctionNotifications.AuctionStatusChangedAsync(auction.Id, to.ToString(), reason, ct);

        string title = auction.Title;
        (string Title, string Body)? message = to switch
        {
            AuctionStatus.Live when from == AuctionStatus.Scheduled => ("İhale Başladı", $"\"{title}\" ihalesi başladı."),
            AuctionStatus.Live when from == AuctionStatus.Paused => ("İhale Devam Ediyor", $"\"{title}\" ihalesi kaldığı yerden devam ediyor."),
            AuctionStatus.Live => ("İhale Yeniden Açıldı", $"\"{title}\" ihalesi yeniden teklife açıldı."),
            AuctionStatus.Paused => ("İhale Duraklatıldı", WithReason($"\"{title}\" ihalesi geçici olarak durduruldu.", reason)),
            AuctionStatus.Ended => ("İhale Sona Erdi", $"\"{title}\" ihalesi sona erdi."),
            AuctionStatus.Cancelled => ("İhale İptal Edildi", WithReason($"\"{title}\" ihalesi iptal edildi.", reason)),
            AuctionStatus.Draft => ("İhale Yayından Kaldırıldı", $"\"{title}\" ihalesi yayından kaldırıldı."),
            _ => null, // Yayınlama: tedarikçi davet anında zaten bilgilendirildi.
        };

        if (message is null)
        {
            return;
        }

        List<Guid> recipients = auction.Invitations
            .Where(i => i.Status != InvitationStatus.Declined)
            .Select(i => i.CompanyId)
            .Distinct()
            .ToList();

        foreach (Guid companyId in recipients)
        {
            await NotificationCreator.CreateForCompanyAsync(
                _context, _notificationRealtimeService, _dateTime, companyId,
                NotificationType.AuctionStatusChanged,
                message.Value.Title,
                message.Value.Body,
                nameof(Auction), auction.Id, ct);
        }
    }

    private static string WithReason(string text, string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? text : $"{text} Gerekçe: {reason.Trim()}";
}
