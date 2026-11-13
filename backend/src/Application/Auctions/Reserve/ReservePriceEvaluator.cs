using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Currencies;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Reserve;

/// <summary>
/// Rezerv fiyatın ihale toplamına göre durumu.
/// <c>Incomplete</c>: düşük teklifin kazandığı ihalede toplam henüz rezervin altında ama teklifsiz kalem var (kesin değil).
/// </summary>
public enum ReserveStatus
{
    NotSet,
    NoBids,
    Met,
    NotMet,
    Incomplete
}

/// <summary>Bir kalemin miktarı ve sonucu belirleyen birim fiyatı (ihale para biriminde); teklif yoksa <c>null</c>.</summary>
public sealed record ReserveLine(decimal Quantity, decimal? UnitPrice);

/// <param name="Total">Teklifi olan kalemlerin birim fiyat × miktar toplamı (ihale para biriminde).</param>
public sealed record ReserveEvaluation(ReserveStatus Status, decimal? Total, int CoveredItems, int ItemCount);

/// <summary>
/// Rezerv fiyat, ihalenin <b>toplamı</b> için alıcının iç sınırıdır (tedarikçiye gösterilmez, teklifi engellemez):
/// düşük teklifin kazandığı ihalelerde toplam rezervi aşmamalı, İngiliz (artan) ihalede rezervin altında kalmamalıdır.
/// Sınır karşılanmıyorsa kazanan seçimi ancak açık onayla yapılır (bkz. AwardAuctionItemCommandHandler).
/// Kalem sonucu: ödül verilmişse ödüllü teklif, değilse en iyi teklif.
/// </summary>
public static class ReservePriceEvaluator
{
    public static ReserveEvaluation Evaluate(decimal? reservePrice, bool higherIsBetter, IReadOnlyCollection<ReserveLine> lines)
    {
        if (reservePrice is not decimal reserve || reserve <= 0)
        {
            return new ReserveEvaluation(ReserveStatus.NotSet, null, 0, lines.Count);
        }

        List<ReserveLine> covered = lines.Where(l => l.UnitPrice is not null).ToList();
        if (covered.Count == 0)
        {
            return new ReserveEvaluation(ReserveStatus.NoBids, null, 0, lines.Count);
        }

        decimal total = covered.Sum(l => l.Quantity * l.UnitPrice!.Value);

        ReserveStatus status;
        if (higherIsBetter)
        {
            status = total >= reserve ? ReserveStatus.Met : ReserveStatus.NotMet;
        }
        else if (total > reserve)
        {
            status = ReserveStatus.NotMet;
        }
        else
        {
            status = covered.Count == lines.Count ? ReserveStatus.Met : ReserveStatus.Incomplete;
        }

        return new ReserveEvaluation(status, total, covered.Count, lines.Count);
    }

    /// <param name="bids">İhalenin kalemlerine verilmiş tüm teklifler.</param>
    /// <param name="candidate">Kazanan olarak seçilmek üzere olan teklif; kendi kaleminde en iyi teklifin yerine geçer.</param>
    public static async Task<ReserveEvaluation> EvaluateAsync(
        IApplicationDbContext context,
        Auction auction,
        IReadOnlyCollection<AuctionItem> items,
        IReadOnlyCollection<Bid> bids,
        Bid? candidate,
        CancellationToken cancellationToken)
    {
        if (auction.ReservePrice is not > 0)
        {
            return new ReserveEvaluation(ReserveStatus.NotSet, null, 0, items.Count);
        }

        Dictionary<Guid, Guid> awardedBidByItem = await context.AuctionAwards
            .Where(a => a.AuctionId == auction.Id)
            .ToDictionaryAsync(a => a.AuctionItemId, a => a.WinningBidId, cancellationToken);

        ILookup<Guid, Bid> bidsByItem = bids.ToLookup(b => b.AuctionItemId);
        var rateToBase = new Dictionary<Guid, decimal>();
        var lines = new List<ReserveLine>(items.Count);

        foreach (AuctionItem item in items)
        {
            Bid? decisive = null;
            if (awardedBidByItem.TryGetValue(item.Id, out Guid awardedBidId))
            {
                decisive = bidsByItem[item.Id].FirstOrDefault(b => b.Id == awardedBidId);
            }
            else if (candidate is not null && candidate.AuctionItemId == item.Id)
            {
                decisive = candidate;
            }

            decisive ??= BidRanking.Best(bidsByItem[item.Id], auction.AuctionType);

            decimal? unitPrice = decisive is null
                ? null
                : await ToAuctionCurrencyAsync(context, decisive.Amount.Amount, decisive.Amount.CurrencyId, auction.CurrencyId, rateToBase, cancellationToken);

            lines.Add(new ReserveLine(item.Quantity, unitPrice));
        }

        return Evaluate(auction.ReservePrice, BidRanking.HigherIsBetter(auction.AuctionType), lines);
    }

    private static async Task<decimal> ToAuctionCurrencyAsync(
        IApplicationDbContext context,
        decimal amount,
        Guid currencyId,
        Guid auctionCurrencyId,
        Dictionary<Guid, decimal> rateToBase,
        CancellationToken cancellationToken)
    {
        if (currencyId == auctionCurrencyId)
        {
            return amount;
        }

        decimal from = await RateAsync(context, currencyId, rateToBase, cancellationToken);
        decimal to = await RateAsync(context, auctionCurrencyId, rateToBase, cancellationToken);

        return to == 0 ? amount * from : amount * from / to;
    }

    private static async Task<decimal> RateAsync(
        IApplicationDbContext context, Guid currencyId, Dictionary<Guid, decimal> cache, CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue(currencyId, out decimal rate))
        {
            rate = await CurrencyNormalizer.NormalizeToBaseCurrencyAsync(context, 1m, currencyId, cancellationToken);
            cache[currencyId] = rate;
        }

        return rate;
    }
}

/// <summary>
/// Bir kalemin "en iyi" teklifi. İngiliz ve ters ihalede her yeni teklif öncekini geçmek zorundadır, dolayısıyla tek bir
/// <see cref="BidStatus.Winning"/> teklif vardır. Kapalı zarfta teklifler birbirini geçmez (hepsi Winning kalır), bu yüzden
/// en iyi teklif tutara göre belirlenir.
/// </summary>
public static class BidRanking
{
    public static bool HigherIsBetter(AuctionType type) => type == AuctionType.English;

    public static Bid? Best(IEnumerable<Bid> bids, AuctionType type)
    {
        if (type != AuctionType.SealedBid)
        {
            return bids.FirstOrDefault(b => b.Status == BidStatus.Winning);
        }

        // Geri çekilmiş ya da reddedilmiş teklif sıralamaya girmez; aynı tutarda ilk verilen teklif öndedir.
        return bids
            .Where(b => b.Status is not (BidStatus.Withdrawn or BidStatus.Rejected))
            .OrderBy(b => b.Amount.Amount)
            .ThenBy(b => b.BidTime)
            .FirstOrDefault();
    }
}
