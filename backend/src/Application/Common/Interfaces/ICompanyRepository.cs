using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Common.Interfaces;

public interface ICompanyRepository : IRepository<Company>
{
    Task<bool> ExistsByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken);

    Task<Company?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken);
}
