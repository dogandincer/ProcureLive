using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Status;

/// <summary>
/// İhale durum makinesi — durum değişikliklerinin TEK kaynağı. Elle (admin) geçişlerin listesi burada tanımlıdır;
/// otomatik geçişler (zamanlayıcı: Planlandı→Canlı, Canlı→Sona Erdi; son kalemin kazananı: →Sonuçlandı) de
/// <see cref="Apply"/> üzerinden geçer, böylece tarih alanları ve durum geçmişi her yolda aynı kurala uyar.
/// </summary>
public static class AuctionStatusMachine
{
    private static readonly Dictionary<AuctionStatus, AuctionStatus[]> ManualTransitions = new()
    {
        [AuctionStatus.Draft] = [AuctionStatus.Scheduled, AuctionStatus.Cancelled],
        [AuctionStatus.Scheduled] = [AuctionStatus.Live, AuctionStatus.Draft, AuctionStatus.Cancelled],
        [AuctionStatus.Live] = [AuctionStatus.Paused, AuctionStatus.Ended, AuctionStatus.Cancelled],
        [AuctionStatus.Paused] = [AuctionStatus.Live, AuctionStatus.Ended, AuctionStatus.Cancelled],
        [AuctionStatus.Ended] = [AuctionStatus.Live, AuctionStatus.Cancelled],
        [AuctionStatus.Awarded] = [],
        [AuctionStatus.Cancelled] = [],
    };

    /// <summary>Katılıma açık (devam eden) durumlar: duraklatılmış ihale de bitmiş sayılmaz.</summary>
    public static readonly AuctionStatus[] OpenStatuses = [AuctionStatus.Scheduled, AuctionStatus.Live, AuctionStatus.Paused];

    public static bool IsOpen(AuctionStatus status) => OpenStatuses.Contains(status);

    public static IReadOnlyList<AuctionStatus> AllowedFrom(AuctionStatus from) =>
        ManualTransitions.TryGetValue(from, out AuctionStatus[]? targets) ? targets : [];

    public static bool CanTransition(AuctionStatus from, AuctionStatus to) => AllowedFrom(from).Contains(to);

    /// <summary>Tedarikçiyi doğrudan etkileyen kesintilerde gerekçe zorunlu (bildirimde gösterilir).</summary>
    public static bool RequiresReason(AuctionStatus to) => to is AuctionStatus.Cancelled or AuctionStatus.Paused;

    /// <summary>Bitmiş ihaleyi yeniden açarken yeni bitiş zamanı zorunludur.</summary>
    public static bool RequiresEndAt(AuctionStatus from, AuctionStatus to) =>
        from == AuctionStatus.Ended && to == AuctionStatus.Live;

    public static bool AllowsEndAt(AuctionStatus from, AuctionStatus to) =>
        to == AuctionStatus.Scheduled || to == AuctionStatus.Live;

    public static bool AllowsStartAt(AuctionStatus from, AuctionStatus to) =>
        from == AuctionStatus.Draft && to == AuctionStatus.Scheduled;

    public static string DisplayName(AuctionStatus status) => status switch
    {
        AuctionStatus.Draft => "Taslak",
        AuctionStatus.Scheduled => "Planlandı",
        AuctionStatus.Live => "Canlı",
        AuctionStatus.Paused => "Duraklatıldı",
        AuctionStatus.Ended => "Sona Erdi",
        AuctionStatus.Cancelled => "İptal Edildi",
        AuctionStatus.Awarded => "Sonuçlandı",
        _ => status.ToString(),
    };

    /// <summary>
    /// Durumu ve ilgili tarihleri günceller, geçmişe satır ekler. Doğrulama çağırana aittir
    /// (elle geçişte AuctionStatusService, otomatikte zamanlayıcının sorgu koşulları).
    /// Erken başlatma/bitirmede StartAt/EndAt gerçek ana çekilir; böylece geri sayımlar ve zamanlayıcı tutarlı kalır.
    /// </summary>
    public static void Apply(
        IApplicationDbContext context,
        Auction auction,
        AuctionStatus to,
        DateTime now,
        Guid? userId,
        string? reason,
        bool automatic,
        DateTime? newStartAt = null,
        DateTime? newEndAt = null)
    {
        AuctionStatus from = auction.Status;

        switch (to)
        {
            case AuctionStatus.Scheduled:
                if (newStartAt is DateTime start) auction.StartAt = start;
                if (newEndAt is DateTime end) auction.EndAt = end;
                break;

            case AuctionStatus.Live when from == AuctionStatus.Scheduled:
                if (auction.StartAt > now) auction.StartAt = now;
                auction.ActualStartAt = now;
                if (newEndAt is DateTime liveEnd) auction.EndAt = liveEnd;
                break;

            case AuctionStatus.Live when from == AuctionStatus.Paused:
                // Duraklamada geçen süre tedarikçilerden "çalınmasın": bitişe eklenir (yeni bitiş verilmediyse).
                auction.EndAt = newEndAt ?? auction.EndAt + (now - (auction.PausedAt ?? now));
                auction.PausedAt = null;
                break;

            case AuctionStatus.Live when from == AuctionStatus.Ended:
                auction.EndAt = newEndAt ?? auction.EndAt;
                auction.ActualEndAt = null;
                break;

            case AuctionStatus.Paused:
                auction.PausedAt = now;
                break;

            case AuctionStatus.Ended:
                if (auction.EndAt > now) auction.EndAt = now;
                auction.ActualEndAt = now;
                auction.PausedAt = null;
                break;

            case AuctionStatus.Cancelled:
                if (from is AuctionStatus.Live or AuctionStatus.Paused) auction.ActualEndAt = now;
                auction.PausedAt = null;
                break;
        }

        auction.Status = to;

        // DbSet üzerinden eklenir: BaseEntity Id'yi önceden ürettiği için takip edilen ihalenin koleksiyonuna
        // eklenen satırı EF "mevcut kayıt" sanıp UPDATE dener (0 satır → DbUpdateConcurrencyException).
        context.AuctionStatusChanges.Add(new AuctionStatusChange
        {
            AuctionId = auction.Id,
            FromStatus = from,
            ToStatus = to,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ChangedByUserId = userId,
            IsAutomatic = automatic,
            ChangedAt = now,
        });
    }
}
