using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Reserve;
using ProcureLive.Application.Common.Currencies;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Monitoring;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Monitoring.Queries.GetMonitoringStats;

public sealed class GetMonitoringStatsQueryHandler
    : IRequestHandler<GetMonitoringStatsQuery, MonitoringStatsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly IDataScopeService _scope;

    public GetMonitoringStatsQueryHandler(IApplicationDbContext context, IDateTime dateTime, IDataScopeService scope)
    {
        _context = context;
        _dateTime = dateTime;
        _scope = scope;
    }

    public async Task<MonitoringStatsDto> Handle(
        GetMonitoringStatsQuery request,
        CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;
        DateTime todayStart = now.Date;
        DateTime weekStart = now.Date.AddDays(-7);

        // Satınalmacı yalnızca yetkili işyerlerinin ihalelerini, RFQ'larını ve bunların tekliflerini görür.
        DataScope scope = await _scope.GetAsync(cancellationToken);
        IQueryable<Auction> auctions = _context.Auctions.InScope(scope);
        IQueryable<Guid> auctionIds = auctions.Select(a => a.Id);
        IQueryable<Bid> bids = scope.IsUnrestricted
            ? _context.Bids
            : _context.Bids.Where(b => auctionIds.Contains(b.AuctionItem.AuctionId));
        IQueryable<ProcureLive.Domain.Entities.Rfq.RfqProcess> rfqs = _context.RfqProcesses.InScope(scope);

        // Auction counts by status
        var auctionGroups = await auctions
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        Dictionary<string, int> auctionByStatus = auctionGroups.ToDictionary(g => g.Status, g => g.Count);

        // RFQ counts by status
        var rfqGroups = await rfqs
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        Dictionary<string, int> rfqByStatus = rfqGroups.ToDictionary(g => g.Status, g => g.Count);

        // Bid counts
        int bidsToday = await bids
            .CountAsync(b => b.BidTime >= todayStart, cancellationToken);

        int bidsThisWeek = await bids
            .CountAsync(b => b.BidTime >= weekStart, cancellationToken);

        // Live auctions summary
        List<Auction> liveAuctions = await auctions
            .AsNoTracking()
            .Include(a => a.Items).ThenInclude(i => i.Bids).ThenInclude(b => b.Company)
            .Include(a => a.Currency)
            .Where(a => a.Status == AuctionStatus.Live)
            .OrderBy(a => a.EndAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var liveSummaries = liveAuctions.Select(a =>
        {
            // Kalem liderleri (en iyi teklif; kapalı zarfta tutara göre): en çok kalemde önde olan firma lider; tutar = liderlerin birim fiyat × miktar toplamı.
            var leaders = a.Items
                .Select(i => new { Item = i, Bid = BidRanking.Best(i.Bids, a.AuctionType) })
                .Where(x => x.Bid is not null)
                .ToList();
            string? leadingCompany = leaders
                .GroupBy(x => x.Bid!.Company.Name)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Min(x => x.Bid!.BidTime))
                .Select(g => g.Key)
                .FirstOrDefault();
            decimal? bestTotal = leaders.Count == 0 ? null : leaders.Sum(x => x.Bid!.Amount.Amount * x.Item.Quantity);

            int bidCount = a.Items.SelectMany(i => i.Bids).Count();

            return new LiveAuctionSummaryDto(
                a.Id,
                a.Title,
                bidCount,
                bestTotal,
                a.Currency.Code,
                leadingCompany,
                a.EndAt);
        }).ToList();

        // Recent bids (last 15 across all active bids)
        var recentBidRows = await bids
            .AsNoTracking()
            .Include(b => b.Company)
            .Include(b => b.AuctionItem).ThenInclude(i => i.Auction).ThenInclude(a => a.Currency)
            .OrderByDescending(b => b.BidTime)
            .Take(15)
            .ToListAsync(cancellationToken);

        List<RecentBidDto> recentBids = recentBidRows.Select(b => new RecentBidDto(
            b.Id,
            b.AuctionItem.Auction.Title,
            b.Company.Name,
            b.Amount.Amount,
            b.AuctionItem.Auction.Currency.Code,
            b.BidTime)).ToList();

        return new MonitoringStatsDto(
            auctionByStatus,
            rfqByStatus,
            bidsToday,
            bidsThisWeek,
            liveSummaries,
            recentBids);
    }
}
