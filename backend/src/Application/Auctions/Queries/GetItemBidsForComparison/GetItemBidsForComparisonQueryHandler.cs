using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Currencies;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Application.Auctions.Queries.GetItemBidsForComparison;

public sealed class GetItemBidsForComparisonQueryHandler
    : IRequestHandler<GetItemBidsForComparisonQuery, IReadOnlyCollection<BidComparisonDto>>
{
    private readonly IApplicationDbContext _context;

    public GetItemBidsForComparisonQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<BidComparisonDto>> Handle(
        GetItemBidsForComparisonQuery request,
        CancellationToken cancellationToken)
    {
        List<Bid> bids = await _context.Bids
            .Include(b => b.Company)
            .Where(b => b.AuctionItemId == request.AuctionItemId)
            .OrderByDescending(b => b.BidTime)
            .ToListAsync(cancellationToken);

        var result = new List<BidComparisonDto>();

        foreach (Bid bid in bids)
        {
            decimal normalized = await CurrencyNormalizer.NormalizeToBaseCurrencyAsync(
                _context, bid.Amount.Amount, bid.Amount.CurrencyId, cancellationToken);

            string currencyCode = await _context.Currencies
                .Where(c => c.Id == bid.Amount.CurrencyId)
                .Select(c => c.Code)
                .FirstAsync(cancellationToken);

            result.Add(new BidComparisonDto(
                bid.Id,
                bid.CompanyId,
                bid.Company.Name,
                bid.Amount.Amount,
                currencyCode,
                normalized,
                bid.BidTime,
                bid.Status.ToString()));
        }

        return result;
    }
}
