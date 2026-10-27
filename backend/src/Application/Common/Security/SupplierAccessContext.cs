using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Security;

public enum AuctionAccessLevel
{
    /// <summary>Tüm detay görünür.</summary>
    Full,

    /// <summary>Görünür ama Standart katılım kotası dolu: hassas alanlar maskelenir, istemci bulanıklaştırır.</summary>
    QuotaExceeded,

    /// <summary>Görünürlük kapsamı dışında: neredeyse hiçbir veri dönülmez.</summary>
    NoAccess
}

/// <summary>Erişim kararı için gereken ihale bilgileri (entity'yi taşımadan).</summary>
public readonly record struct AuctionAccessFacts(
    Guid AuctionId,
    AuctionStatus Status,
    Guid OwnerCompanyId,
    bool IsListedForPlus,
    bool OwnerListsForPlus);

/// <summary>
/// Bir tedarikçi carinin ihale erişim/kota durumu. Liste, detay, katılım ve SignalR aynı kuralı
/// buradan uygular — kural tek yerde değişir.
///
/// Görünürlük (Standart): davet edildiği ihaleler + kendisini davet eden firmanın açık (Planlı/Canlı) ihaleleri.
/// Plus ek olarak: Plus'a listelenmiş (ihale + sahibi firma onaylı) açık ihaleler.
/// Kota: Standart cari en fazla <see cref="StandardJoinLimit"/> ihaleye katılabilir (kabul edilmiş katılımlar, kümülatif).
/// Reddedilen ihaleye her durumda erişim yoktur.
/// </summary>
public sealed class SupplierAccessContext
{
    public const int StandardJoinLimit = 2;

    public required Guid CompanyId { get; init; }

    public required bool IsPlus { get; init; }

    /// <summary>Reddedilmemiş davetler (Invited dahil).</summary>
    public required IReadOnlySet<Guid> InvitedAuctionIds { get; init; }

    /// <summary>Katıldığı ihaleler (Accepted/Participating/Won/Lost) — kotayı tüketir.</summary>
    public required IReadOnlySet<Guid> JoinedAuctionIds { get; init; }

    public required IReadOnlySet<Guid> DeclinedAuctionIds { get; init; }

    /// <summary>Bu cariyi (reddedilmemiş bir davetle) davet etmiş sahip firmalar.</summary>
    public required IReadOnlySet<Guid> InvitingOwnerIds { get; init; }

    /// <summary>Carinin onaylı olduğu (birincil + ek görünür) işyerleri ve bu işyerlerinin firmaları.</summary>
    public IReadOnlySet<Guid> ApprovedWorkspaceIds { get; init; } = new HashSet<Guid>();

    public IReadOnlySet<Guid> ApprovedFirmIds { get; init; } = new HashSet<Guid>();

    /// <summary>Pasif cari hiçbir işyerinde onaylı sayılmaz.</summary>
    public bool IsCompanyActive { get; init; } = true;

    /// <summary>İhale bazında son katılım talebinin durumu.</summary>
    public IReadOnlyDictionary<Guid, ParticipationRequestStatus> RequestStatuses { get; init; } =
        new Dictionary<Guid, ParticipationRequestStatus>();

    public int JoinedCount => JoinedAuctionIds.Count;

    public bool QuotaReached => !IsPlus && JoinedCount >= StandardJoinLimit;

    public int? RemainingJoins => IsPlus ? null : Math.Max(0, StandardJoinLimit - JoinedCount);

    public static bool IsOpen(AuctionStatus status) => Auctions.Status.AuctionStatusMachine.IsOpen(status);

    public bool IsVisible(AuctionAccessFacts a)
    {
        if (DeclinedAuctionIds.Contains(a.AuctionId)) return false;

        return InvitedAuctionIds.Contains(a.AuctionId)
            || JoinedAuctionIds.Contains(a.AuctionId)
            || (IsOpen(a.Status) && InvitingOwnerIds.Contains(a.OwnerCompanyId))
            || (IsPlus && IsOpen(a.Status) && a.IsListedForPlus && a.OwnerListsForPlus);
    }

    public AuctionAccessLevel Evaluate(AuctionAccessFacts a)
    {
        if (DeclinedAuctionIds.Contains(a.AuctionId)) return AuctionAccessLevel.NoAccess;
        if (JoinedAuctionIds.Contains(a.AuctionId)) return AuctionAccessLevel.Full;
        if (!IsVisible(a)) return AuctionAccessLevel.NoAccess;
        return QuotaReached ? AuctionAccessLevel.QuotaExceeded : AuctionAccessLevel.Full;
    }

    public bool IsJoined(Guid auctionId) => JoinedAuctionIds.Contains(auctionId);

    /// <summary>Henüz katılmamış, açık, görünür ve kota uygun ihaleye katılabilir (doğrudan ya da talep ile).</summary>
    public bool CanJoin(AuctionAccessFacts a) =>
        !IsJoined(a.AuctionId) && IsOpen(a.Status) && Evaluate(a) == AuctionAccessLevel.Full;

    /// <summary>
    /// Cari, ihalenin işyerinde (işyeri yoksa sahip firmanın herhangi bir işyerinde) görünür ve aktifse "onaylı"dır.
    /// </summary>
    public bool IsApprovedFor(Guid? auctionWorkspaceId, Guid ownerFirmId) =>
        IsCompanyActive && (auctionWorkspaceId is Guid w ? ApprovedWorkspaceIds.Contains(w) : ApprovedFirmIds.Contains(ownerFirmId));

    public ParticipationRequestStatus? RequestStatus(Guid auctionId) =>
        RequestStatuses.TryGetValue(auctionId, out ParticipationRequestStatus s) ? s : null;

    public bool HasActiveRequest(Guid auctionId) =>
        RequestStatus(auctionId) is ParticipationRequestStatus.Pending or ParticipationRequestStatus.CompanyApproved;

    /// <summary>Davetsiz ve onaylı cari değilse katılım için satınalmacı onayı (katılım talebi) gerekir.</summary>
    public bool NeedsApproval(AuctionAccessFacts a, Guid? auctionWorkspaceId) =>
        !InvitedAuctionIds.Contains(a.AuctionId) && !IsApprovedFor(auctionWorkspaceId, a.OwnerCompanyId);
}
