using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Currencies;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Monitoring;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Monitoring.Queries.GetMonitoringStats;

public sealed class GetMonitoringStatsQueryHandler
    : IRequestHandler<GetMonitoringStatsQuery, MonitoringStatsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public GetMonitoringStatsQueryHandler(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<MonitoringStatsDto> Handle(
        GetMonitoringStatsQuery request,
        CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;
        DateTime todayStart = now.Date;
        DateTime weekStart = now.Date.AddDays(-7);

        // Auction counts by status
        var auctionGroups = await _context.Auctions
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        Dictionary<string, int> auctionByStatus = auctionGroups.ToDictionary(g => g.Status, g => g.Count);

        // RFQ counts by status
        var rfqGroups = await _context.RfqProcesses
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        Dictionary<string, int> rfqByStatus = rfqGroups.ToDictionary(g => g.Status, g => g.Count);

        // Bid counts
        int bidsToday = await _context.Bids
            .CountAsync(b => b.BidTime >= todayStart, cancellationToken);

        int bidsThisWeek = await _context.Bids
            .CountAsync(b => b.BidTime >= weekStart, cancellationToken);

        // Live auctions summary
        List<Auction> liveAuctions = await _context.Auctions
            .AsNoTracking()
            .Include(a => a.Items).ThenInclude(i => i.Bids).ThenInclude(b => b.Company)
            .Include(a => a.Currency)
            .Where(a => a.Status == AuctionStatus.Live)
            .OrderBy(a => a.EndAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var liveSummaries = liveAuctions.Select(a =>
        {
            // Find leading bid: lowest amount active bid across all items
            Bid? bestBid = a.Items
                .SelectMany(i => i.Bids)
                .Where(b => b.Status == BidStatus.Active)
                .OrderBy(b => b.Amount.Amount)
                .FirstOrDefault();

            int bidCount = a.Items.SelectMany(i => i.Bids).Count();

            return new LiveAuctionSummaryDto(
                a.Id,
                a.Title,
                bidCount,
                bestBid?.Amount.Amount,
                a.Currency.Code,
                bestBid?.Company.Name,
                a.EndAt);
        }).ToList();

        // Recent bids (last 15 across all active bids)
        var recentBidRows = await _context.Bids
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
