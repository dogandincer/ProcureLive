using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Companies;

namespace ProcureLive.Application.Sectors.Queries.GetSectorsList;

public sealed class GetSectorsListQueryHandler : IRequestHandler<GetSectorsListQuery, List<SectorDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSectorsListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SectorDto>> Handle(GetSectorsListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ProcureLive.Domain.Entities.Companies.Sector> query = _context.Sectors.AsQueryable();

        if (request.ActiveOnly == true)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SectorDto(
                s.Id,
                s.Name,
                s.Description,
                s.IsActive,
                s.Companies.Count))
            .ToListAsync(cancellationToken);
    }
}
