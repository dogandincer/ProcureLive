using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Common.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken);

    Task<Product?> GetWithImagesAsync(Guid id, CancellationToken cancellationToken);
}
