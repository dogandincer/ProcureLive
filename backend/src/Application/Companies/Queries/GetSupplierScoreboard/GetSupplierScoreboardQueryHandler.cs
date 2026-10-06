using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Queries.GetSupplierScoreboard;

public sealed class GetSupplierScoreboardQueryHandler
    : IRequestHandler<GetSupplierScoreboardQuery, SupplierScoreboardDto>
{
    private readonly IApplicationDbContext _context;

    public GetSupplierScoreboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupplierScoreboardDto> Handle(GetSupplierScoreboardQuery request, CancellationToken cancellationToken)
    {
        // Companies that have at least one evaluation, with their evaluations and sector names.
        List<Company> evaluated = await _context.Companies
            .AsNoTracking()
            .Include(c => c.Evaluations)
            .Include(c => c.Sectors).ThenInclude(cs => cs.Sector)
            .Where(c => c.Evaluations.Any())
            .ToListAsync(cancellationToken);

        var scores = evaluated
            .Select(c => new SupplierScoreDto(
                c.Id,
                c.Name,
                Math.Round(c.Evaluations.Average(e => e.OverallScore), 2),
                c.Evaluations.Count,
                Math.Round((decimal)c.Evaluations.Average(e => e.QualityScore), 2),
                Math.Round((decimal)c.Evaluations.Average(e => e.DeliveryScore), 2),
                Math.Round((decimal)c.Evaluations.Average(e => e.CommunicationScore), 2),
                Math.Round((decimal)c.Evaluations.Average(e => e.PriceScore), 2),
                Math.Round((decimal)c.Evaluations.Average(e => e.ComplianceScore), 2),
                c.Sectors.Select(cs => cs.Sector.Name).OrderBy(n => n).ToList()))
            .OrderByDescending(s => s.AverageScore)
            .ThenByDescending(s => s.EvaluationCount)
            .ToList();

        // Quick lookup of a company's average score for the per-sector ranking.
        Dictionary<Guid, (decimal Avg, int Count)> avgByCompany = scores
            .ToDictionary(s => s.CompanyId, s => (s.AverageScore, s.EvaluationCount));

        // Sectors with their member companies (soft-deleted companies are filtered out by the global query filter).
        List<Sector> sectors = await _context.Sectors
            .AsNoTracking()
            .Include(s => s.Companies).ThenInclude(cs => cs.Company)
            .ToListAsync(cancellationToken);

        var popularSectors = new List<SectorPopularityDto>();
        var topPerSector = new List<TopSupplierPerSectorDto>();

        foreach (Sector sector in sectors)
        {
            List<Company> members = sector.Companies.Select(cs => cs.Company).Where(c => c != null).ToList();
            List<Company> evaluatedMembers = members.Where(c => avgByCompany.ContainsKey(c.Id)).ToList();

            decimal? sectorAvg = evaluatedMembers.Count > 0
                ? Math.Round(evaluatedMembers.Average(c => avgByCompany[c.Id].Avg), 2)
                : (decimal?)null;

            popularSectors.Add(new SectorPopularityDto(
                sector.Id, sector.Name, members.Count, evaluatedMembers.Count, sectorAvg));

            Company? best = evaluatedMembers
                .OrderByDescending(c => avgByCompany[c.Id].Avg)
                .ThenByDescending(c => avgByCompany[c.Id].Count)
                .FirstOrDefault();

            topPerSector.Add(new TopSupplierPerSectorDto(
                sector.Id,
                sector.Name,
                best?.Id,
                best?.Name,
                best != null ? avgByCompany[best.Id].Avg : (decimal?)null,
                best != null ? avgByCompany[best.Id].Count : 0));
        }

        // Most popular first: by member count, then by evaluated-member count.
        popularSectors = popularSectors
            .OrderByDescending(s => s.CompanyCount)
            .ThenByDescending(s => s.EvaluatedCompanyCount)
            .ToList();

        // Per-sector table: sectors that actually have a top supplier first, best score first.
        topPerSector = topPerSector
            .OrderByDescending(t => t.CompanyId.HasValue)
            .ThenByDescending(t => t.AverageScore ?? -1)
            .ToList();

        int totalEvaluations = evaluated.Sum(c => c.Evaluations.Count);

        return new SupplierScoreboardDto(
            TopSuppliers: scores.Take(request.TopCount).ToList(),
            PopularSectors: popularSectors,
            TopSupplierPerSector: topPerSector,
            TotalEvaluatedSuppliers: scores.Count,
            TotalEvaluations: totalEvaluations);
    }
}
