using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Companies.Queries.GetCompaniesList;

public sealed class GetCompaniesListQueryHandler : IRequestHandler<GetCompaniesListQuery, PagedResult<CompanyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCompaniesListQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<CompanyDto>> Handle(GetCompaniesListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Company> query = _context.Companies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            string term = request.SearchTerm.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.TaxNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.CompanyType))
        {
            CompanyType companyType;
            bool parsed = Enum.TryParse(request.CompanyType, true, out companyType);

            if (parsed)
            {
                query = query.Where(c => c.CompanyType == companyType);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            CompanyStatus status;
            bool parsed = Enum.TryParse(request.Status, true, out status);

            if (parsed)
            {
                query = query.Where(c => c.Status == status);
            }
        }

        if (request.SectorId.HasValue)
        {
            query = query.Where(c => c.Sectors.Any(cs => cs.SectorId == request.SectorId.Value));
        }

        if (request.WorkspaceId.HasValue)
        {
            query = query.Where(c => c.WorkspaceId == request.WorkspaceId.Value);
        }
        else if (request.FirmId.HasValue)
        {
            // İşyeri seçilmemişse ("Tüm İşyerleri") başlıktaki firmanın işyerleriyle sınırlanır.
            query = query.Where(c => c.Workspace != null && c.Workspace.FirmId == request.FirmId.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        List<CompanyDto> items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<CompanyDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        // Fill rating aggregates with a single grouped query over the page's companies.
        List<Guid> ids = items.Select(i => i.Id).ToList();
        Dictionary<Guid, (decimal Avg, int Count)> ratings = await _context.CompanyEvaluations
            .Where(e => ids.Contains(e.CompanyId))
            .GroupBy(e => e.CompanyId)
            .Select(g => new { CompanyId = g.Key, Avg = g.Average(x => x.OverallScore), Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => (x.Avg, x.Count), cancellationToken);

        items = items
            .Select(i => ratings.TryGetValue(i.Id, out var r)
                ? i with { AverageScore = Math.Round(r.Avg, 2), EvaluationCount = r.Count }
                : i)
            .ToList();

        return new PagedResult<CompanyDto>(items, totalCount, page, pageSize);
    }
}
