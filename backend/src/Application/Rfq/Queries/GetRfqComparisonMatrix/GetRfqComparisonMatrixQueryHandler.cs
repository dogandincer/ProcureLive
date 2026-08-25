using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Currencies;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Rfq;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Queries.GetRfqComparisonMatrix;

public sealed class GetRfqComparisonMatrixQueryHandler
    : IRequestHandler<GetRfqComparisonMatrixQuery, IReadOnlyCollection<RfqComparisonRowDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRfqComparisonMatrixQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<RfqComparisonRowDto>> Handle(
        GetRfqComparisonMatrixQuery request,
        CancellationToken cancellationToken)
    {
        List<RfqQuoteLineItem> lines = await _context.RfqQuoteLineItems
            .Include(l => l.RfqLineItem)
            .Include(l => l.RfqQuote).ThenInclude(q => q.Company)
            .Where(l => l.RfqQuote.RfqProcessId == request.RfqProcessId && l.RfqQuote.Status == RfqQuoteStatus.Submitted)
            .ToListAsync(cancellationToken);

        var result = new List<RfqComparisonRowDto>();

        foreach (RfqQuoteLineItem line in lines)
        {
            decimal normalized = await CurrencyNormalizer.NormalizeToBaseCurrencyAsync(
                _context, line.UnitPrice.Amount, line.UnitPrice.CurrencyId, cancellationToken);

            string currencyCode = await _context.Currencies
                .Where(c => c.Id == line.UnitPrice.CurrencyId)
                .Select(c => c.Code)
                .FirstAsync(cancellationToken);

            result.Add(new RfqComparisonRowDto(
                line.RfqLineItemId, line.RfqLineItem.Description, line.RfqQuote.CompanyId, line.RfqQuote.Company.Name,
                line.UnitPrice.Amount, currencyCode, normalized, line.LeadTimeDays));
        }

        return result
            .OrderBy(r => r.LineItemDescription)
            .ThenBy(r => r.NormalizedUnitPriceInBaseCurrency)
            .ToList();
    }
}
