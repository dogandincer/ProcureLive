using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Currencies;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Rfq;
using ProcureLive.Domain.Entities.Rfq;

namespace ProcureLive.Application.Rfq.Queries.GetRfqQuotes;

public sealed class GetRfqQuotesQueryHandler : IRequestHandler<GetRfqQuotesQuery, IReadOnlyCollection<RfqQuoteDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRfqQuotesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyCollection<RfqQuoteDto>> Handle(GetRfqQuotesQuery request, CancellationToken cancellationToken)
    {
        bool isInternalUser = _currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.Buyer);

        IQueryable<RfqQuote> query = _context.RfqQuotes
            .Include(q => q.Company)
            .Include(q => q.LineItems).ThenInclude(l => l.RfqLineItem)
            .Where(q => q.RfqProcessId == request.RfqProcessId);

        if (!isInternalUser)
        {
            Guid companyId = _currentUserService.CompanyId ?? Guid.Empty;
            query = query.Where(q => q.CompanyId == companyId);
        }

        List<RfqQuote> quotes = await query.ToListAsync(cancellationToken);

        var result = new List<RfqQuoteDto>();

        foreach (RfqQuote quote in quotes)
        {
            var lineItemDtos = new List<RfqQuoteLineItemDto>();

            foreach (RfqQuoteLineItem line in quote.LineItems)
            {
                decimal normalized = await CurrencyNormalizer.NormalizeToBaseCurrencyAsync(
                    _context, line.UnitPrice.Amount, line.UnitPrice.CurrencyId, cancellationToken);

                string currencyCode = await _context.Currencies
                    .Where(c => c.Id == line.UnitPrice.CurrencyId)
                    .Select(c => c.Code)
                    .FirstAsync(cancellationToken);

                lineItemDtos.Add(new RfqQuoteLineItemDto(
                    line.RfqLineItemId, line.RfqLineItem.Description, line.UnitPrice.Amount,
                    line.UnitPrice.CurrencyId, currencyCode, normalized, line.LeadTimeDays, line.Notes));
            }

            result.Add(new RfqQuoteDto(
                quote.Id, quote.CompanyId, quote.Company.Name, quote.Status.ToString(),
                quote.SubmittedAt, quote.ValidUntil, quote.Notes, lineItemDtos));
        }

        return result;
    }
}
