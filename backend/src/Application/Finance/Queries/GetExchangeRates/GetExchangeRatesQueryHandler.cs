using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Finance;
using ProcureLive.Domain.Entities.Finance;

namespace ProcureLive.Application.Finance.Queries.GetExchangeRates;

public sealed class GetExchangeRatesQueryHandler
    : IRequestHandler<GetExchangeRatesQuery, IReadOnlyCollection<ExchangeRateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetExchangeRatesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<ExchangeRateDto>> Handle(
        GetExchangeRatesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<ExchangeRate> query = _context.ExchangeRates.AsQueryable();

        if (request.CurrencyId.HasValue)
        {
            query = query.Where(r => r.CurrencyId == request.CurrencyId.Value);
        }

        if (request.Date.HasValue)
        {
            query = query.Where(r => r.RateDate == request.Date.Value);
        }

        return await query
            .OrderByDescending(r => r.RateDate)
            .ProjectTo<ExchangeRateDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
