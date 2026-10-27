using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Workspaces;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Infrastructure.Persistence.Seed;

/// <summary>
/// Firma/İşyeri değişmezlerini başlangıçta sağlar (idempotent): firmasız eski işyerleri varsayılan firmaya bağlanır
/// (hiç firma yoksa bir tane oluşturulur), kodsuz firmalara kod üretilir, tek varsayılan firma ve firma başına
/// tek varsayılan işyeri garanti edilir.
/// </summary>
public static class FirmBootstrapper
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ApplicationDbContext context = services.GetRequiredService<ApplicationDbContext>();

        List<Company> firms = await context.Companies
            .Where(c => c.CompanyType == CompanyType.Internal)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
        List<Workspace> workspaces = await context.Workspaces.OrderBy(w => w.CreatedAt).ToListAsync(cancellationToken);

        if (firms.Count == 0 && workspaces.Any(w => w.FirmId == null))
        {
            var firm = new Company
            {
                Code = "FIRMA-001",
                Name = "Varsayılan Firma",
                CompanyType = CompanyType.Internal,
                Status = CompanyStatus.Active,
                IsDefaultFirm = true,
            };
            context.Companies.Add(firm);
            firms.Add(firm);
        }

        HashSet<string> usedCodes = (await context.Companies.IgnoreQueryFilters()
            .Where(c => c.Code != null).Select(c => c.Code!).ToListAsync(cancellationToken)).ToHashSet();
        int next = 1;
        foreach (Company firm in firms.Where(f => string.IsNullOrWhiteSpace(f.Code)))
        {
            string code;
            do { code = $"FIRMA-{next++:000}"; } while (usedCodes.Contains(code));
            usedCodes.Add(code);
            firm.Code = code;
        }

        if (firms.Count > 0)
        {
            Company defaultFirm = firms.FirstOrDefault(f => f.IsDefaultFirm) ?? firms[0];
            firms.ForEach(f => f.IsDefaultFirm = f == defaultFirm);

            foreach (Workspace orphan in workspaces.Where(w => w.FirmId == null))
            {
                orphan.FirmId = defaultFirm.Id;
            }
        }

        foreach (IGrouping<Guid?, Workspace> group in workspaces.Where(w => w.FirmId != null).GroupBy(w => w.FirmId))
        {
            Workspace keep = group.FirstOrDefault(w => w.IsDefault) ?? group.First();
            foreach (Workspace w in group) w.IsDefault = w == keep;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
