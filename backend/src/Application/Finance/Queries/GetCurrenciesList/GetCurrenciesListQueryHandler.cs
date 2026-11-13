using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Mappings;
using ProcureLive.Contracts.Finance;

namespace ProcureLive.Application.Finance.Queries.GetCurrenciesList;

public sealed class GetCurrenciesListQueryHandler : IRequestHandler<GetCurrenciesListQuery, IReadOnlyCollection<CurrencyDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCurrenciesListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<CurrencyDto>> Handle(GetCurrenciesListQuery request, CancellationToken cancellationToken)
    {
        return await _context.Currencies
            .OrderBy(c => c.Code)
            .Select(Projections.Currency)
            .ToListAsync(cancellationToken);
    }
}
