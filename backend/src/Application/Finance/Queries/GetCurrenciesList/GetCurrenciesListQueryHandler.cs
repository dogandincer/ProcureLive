using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Finance;

namespace ProcureLive.Application.Finance.Queries.GetCurrenciesList;

public sealed class GetCurrenciesListQueryHandler : IRequestHandler<GetCurrenciesListQuery, IReadOnlyCollection<CurrencyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCurrenciesListQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<CurrencyDto>> Handle(GetCurrenciesListQuery request, CancellationToken cancellationToken)
    {
        return await _context.Currencies
            .OrderBy(c => c.Code)
            .ProjectTo<CurrencyDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
