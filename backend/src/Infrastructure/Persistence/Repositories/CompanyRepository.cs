using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository : RepositoryBase<Company>, ICompanyRepository
{
    public CompanyRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(c => c.TaxNumber == taxNumber, cancellationToken);
    }

    public async Task<Company?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await DbSet
            .Include(c => c.ContactPersons)
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
