using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Common.Interfaces;

public interface ISupplierAccessService
{
    Task<SupplierAccessContext> GetContextAsync(Guid companyId, CancellationToken cancellationToken);
}
