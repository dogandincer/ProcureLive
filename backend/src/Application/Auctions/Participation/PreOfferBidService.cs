using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Currencies;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;
using ProcureLive.Domain.ValueObjects;

namespace ProcureLive.Application.Auctions.Participation;

/// <summary>
/// Kabul edilmiş katılım taleplerinin ön tekliflerini, ihale canlıyken tedarikçinin ilk teklifi olarak yazar.
/// Çağrıldığı yerler: ihale canlıya geçtiğinde (elle başlatma / zamanlayıcı / devam / yeniden açma) ve ihale zaten
/// canlıyken talep kabul edildiğinde. Her talep bir kez işlenir (PreOfferAppliedAt).
///
/// Normal teklifle aynı kural: mevcut en iyiyi geçmeyen satır teklif olmaz (atlanır; tedarikçi canlı ihalede teklif verir).
/// Aynı kalemde birden fazla ön teklif kötüden iyiye işlenir — hepsi kayda geçer, en iyisi önde kalır; eşitlikte
/// önce kabul edilen önde kalır. Süre uzatma (anti-sniping) tetiklenmez: bu teklifler son dakika teklifi değildir.
/// </summary>
public sealed class PreOfferBidService
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTime _dateTime;
    private readonly IAuctionCacheService _cache;
    private readonly IAuctionNotificationService _auctionNotifications;
    private readonly INotificationRealtimeService _realtime;

    public PreOfferBidService(
        IApplicationDbContext context, IUnitOfWork unitOfWork, IDateTime dateTime, IAuctionCacheService cache,
        IAuctionNotificationService auctionNotifications, INotificationRealtimeService realtime)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _dateTime = dateTime;
        _cache = cache;
        _auctionNotifications = auctionNotifications;
        _realtime = realtime;
    }

    private sealed record Candidate(ParticipationRequest Request, ParticipationRequestLine Line, decimal Normalized);

    /// <returns>Oluşturulan teklif sayısı.</returns>
    public async Task<int> ApplyAsync(Guid auctionId, CancellationToken ct)
    {
        Auction? auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == auctionId, ct);
        DateTime now = _dateTime.UtcNow;
        if (auction is null || auction.Status != AuctionStatus.Live || now >= auction.EndAt)
            return 0;

        List<ParticipationRequest> requests = await _context.ParticipationRequests
            .Include(r => r.Lines)
            .Where(r => r.AuctionId == auctionId && r.Status == ParticipationRequestStatus.Accepted && r.PreOfferAppliedAt == null)
            .OrderBy(r => r.DecidedAt)
            .ToListAsync(ct);
        if (requests.Count == 0)
            return 0;

        bool higherIsBetter = auction.AuctionType == AuctionType.English;
        bool compare = auction.AuctionType != AuctionType.SealedBid;
        List<Guid> itemIds = requests.SelectMany(r => r.Lines).Select(l => l.AuctionItemId).Distinct().OrderBy(id => id).ToList();

        var created = new List<(Bid Bid, decimal Normalized)>();
        var outbidCompanies = new List<(Guid CompanyId, Guid ItemId)>();

        await _unitOfWork.BeginTransactionAsync(ct);

        // Canlı teklif vermeyle aynı kilit: kalem satırları işlem boyunca kilitli (sabit sırayla — kilitlenme olmaz).
        foreach (Guid itemId in itemIds)
            await _context.ExecuteRawSqlAsync("SELECT 1 FROM \"AuctionItems\" WHERE \"Id\" = {0} FOR UPDATE", new object[] { itemId }, ct);

        foreach (Guid itemId in itemIds)
        {
            Bid? best = await _context.Bids.FirstOrDefaultAsync(b => b.AuctionItemId == itemId && b.Status == BidStatus.Winning, ct);
            decimal? bestNormalized = best is null
                ? null
                : await CurrencyNormalizer.NormalizeToBaseCurrencyAsync(_context, best.Amount.Amount, best.Amount.CurrencyId, ct);

            var candidates = new List<Candidate>();
            foreach (ParticipationRequest r in requests)
            foreach (ParticipationRequestLine line in r.Lines.Where(l => l.AuctionItemId == itemId))
            {
                decimal normalized = await CurrencyNormalizer.NormalizeToBaseCurrencyAsync(_context, line.UnitPrice, auction.CurrencyId, ct);
                candidates.Add(new Candidate(r, line, normalized));
            }

            // Kötüden iyiye; eşitlikte önce kabul edilen önce işlenir ve önde kalır.
            IEnumerable<Candidate> ordered = higherIsBetter
                ? candidates.OrderBy(c => c.Normalized).ThenBy(c => c.Request.DecidedAt)
                : candidates.OrderByDescending(c => c.Normalized).ThenBy(c => c.Request.DecidedAt);

            foreach (Candidate c in ordered)
            {
                bool beats = !compare || bestNormalized is null
                    || (higherIsBetter ? c.Normalized > bestNormalized : c.Normalized < bestNormalized);
                if (!beats)
                    continue;

                if (compare && best is not null)
                {
                    best.Status = BidStatus.Outbid;
                    if (best.CompanyId != c.Request.CompanyId)
                        outbidCompanies.Add((best.CompanyId, itemId));
                }

                var bid = new Bid
                {
                    AuctionItemId = itemId,
                    CompanyId = c.Request.CompanyId,
                    UserId = c.Request.RequestedByUserId,
                    Amount = new Money(c.Line.UnitPrice, auction.CurrencyId),
                    BidTime = now,
                    Status = BidStatus.Winning,
                };
                _context.Bids.Add(bid);
                c.Line.BidId = bid.Id;
                created.Add((bid, c.Normalized));

                if (compare)
                {
                    best = bid;
                    bestNormalized = c.Normalized;
                }
            }
        }

        HashSet<Guid> biddingCompanies = created.Select(x => x.Bid.CompanyId).ToHashSet();
        List<AuctionInvitation> invitations = await _context.AuctionInvitations
            .Where(i => i.AuctionId == auctionId && biddingCompanies.Contains(i.CompanyId) && i.Status == InvitationStatus.Accepted)
            .ToListAsync(ct);
        foreach (AuctionInvitation invitation in invitations)
            invitation.Status = InvitationStatus.Participating;

        foreach (ParticipationRequest r in requests)
            r.PreOfferAppliedAt = now;

        await _unitOfWork.CommitTransactionAsync(ct);

        // Önbellek + canlı yayın (açık ihale sayfaları) + kalıcı bildirimler.
        foreach (var group in created.Where(x => x.Bid.Status == BidStatus.Winning).GroupBy(x => x.Bid.AuctionItemId))
            await _cache.SetBestAmountAsync(group.Key, group.Last().Normalized, ct);

        foreach ((Bid bid, _) in created)
        {
            string companyName = await _context.Companies.Where(c => c.Id == bid.CompanyId).Select(c => c.Name).FirstAsync(ct);
            await _auctionNotifications.BidPlacedAsync(auctionId, new BidDto(
                bid.Id, bid.AuctionItemId, bid.CompanyId, companyName, bid.Amount.Amount, bid.Amount.CurrencyId,
                bid.BidTime, bid.Status.ToString(), IsMine: false), auction.AuctionType is AuctionType.SealedBid or AuctionType.ReverseSealed, ct);
        }

        Dictionary<Guid, string> itemNames = await _context.AuctionItems.Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Description, ct);

        foreach (ParticipationRequest r in requests)
        {
            int applied = r.Lines.Count(l => l.BidId != null);
            int skipped = r.Lines.Count - applied;
            string body = $"\"{auction.Title}\" ihalesinde ön teklifiniz {applied} kalemde ilk teklif olarak işlendi.";
            if (skipped > 0)
                body += $" {skipped} kalem mevcut en iyi teklifi geçmediği için işlenmedi; canlı ihalede teklif verebilirsiniz.";

            await NotificationCreator.CreateForCompanyAsync(
                _context, _realtime, _dateTime, r.CompanyId, NotificationType.ParticipationRequest,
                "Ön Teklifiniz İşlendi", body, nameof(Auction), auctionId, ct);
        }

        // Önü geçilen (ve işlem sonunda o kalemde önde olmayan) firmalara "Teklifiniz Geçildi".
        HashSet<(Guid ItemId, Guid CompanyId)> leaders = created
            .Where(x => x.Bid.Status == BidStatus.Winning).Select(x => (x.Bid.AuctionItemId, x.Bid.CompanyId)).ToHashSet();
        foreach ((Guid companyId, Guid itemId) in outbidCompanies.Distinct().Where(o => !leaders.Contains((o.ItemId, o.CompanyId))))
        {
            await NotificationCreator.CreateForCompanyAsync(
                _context, _realtime, _dateTime, companyId, NotificationType.Outbid,
                "Teklifiniz Geçildi",
                $"\"{auction.Title}\" ihalesinde \"{itemNames.GetValueOrDefault(itemId)}\" kalemi için teklifiniz geçildi.",
                nameof(Auction), auctionId, ct);
        }

        if (created.Count > 0)
        {
            await NotificationCreator.CreateForInternalUserAsync(
                _context, _realtime, _dateTime, auction.ResponsibleUserId, NotificationType.ParticipationRequest,
                "Ön Teklifler İşlendi",
                $"\"{auction.Title}\" ihalesinde {biddingCompanies.Count} tedarikçinin ön teklifi ilk teklif olarak işlendi.",
                nameof(Auction), auctionId, ct);
        }

        return created.Count;
    }
}
