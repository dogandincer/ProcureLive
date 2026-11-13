using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Mappings;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Queries.GetCompaniesList;

public sealed class GetCompaniesListQueryHandler : IRequestHandler<GetCompaniesListQuery, PagedResult<CompanyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDataScopeService _scope;

    public GetCompaniesListQueryHandler(IApplicationDbContext context, IDataScopeService scope)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<PagedResult<CompanyDto>> Handle(GetCompaniesListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Company> query = _context.Companies.AsQueryable();

        // Satınalmacı: yetkili işyerlerinde (birincil veya ek) görünen cariler + yetkili firmalar.
        DataScope scope = await _scope.GetAsync(cancellationToken);
        if (!scope.IsUnrestricted)
        {
            List<Guid> workspaceIds = scope.WorkspaceIds.ToList();
            List<Guid> firmIds = scope.FirmIds.ToList();
            query = query.Where(c => (c.WorkspaceId.HasValue && workspaceIds.Contains(c.WorkspaceId.Value))
                || c.VisibleWorkspaces.Any(v => workspaceIds.Contains(v.WorkspaceId))
                || firmIds.Contains(c.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Büyük/küçük harf duyarsız (Türkçe I sorununa karşı terim kültürden bağımsız küçültülür).
            string term = request.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(c => c.Name.ToLower().Contains(term) || c.TaxNumber.Contains(term) || (c.Code != null && c.Code.ToLower().Contains(term)));
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

        // Kendi kaydolmuş, henüz hiçbir işyerine bağlanmamış tedarikçiler Admin'e her firmada listelenir (onay havuzu).
        bool includeUnassigned = scope.IsUnrestricted;

        if (request.WorkspaceId.HasValue)
        {
            Guid workspaceId = request.WorkspaceId.Value;
            query = query.Where(c => c.WorkspaceId == workspaceId
                || c.VisibleWorkspaces.Any(v => v.WorkspaceId == workspaceId)
                || (includeUnassigned && c.CompanyType != CompanyType.Internal && c.WorkspaceId == null && !c.VisibleWorkspaces.Any()));
        }
        else if (request.FirmId.HasValue)
        {
            // İşyeri seçilmemişse ("Tüm İşyerleri") başlıktaki firmanın işyerleriyle sınırlanır.
            Guid firmId = request.FirmId.Value;
            query = query.Where(c => (c.Workspace != null && c.Workspace.FirmId == firmId)
                || c.VisibleWorkspaces.Any(v => v.Workspace.FirmId == firmId)
                || (includeUnassigned && c.CompanyType != CompanyType.Internal && c.WorkspaceId == null && !c.VisibleWorkspaces.Any()));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        List<CompanyDto> items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projections.Company)
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
