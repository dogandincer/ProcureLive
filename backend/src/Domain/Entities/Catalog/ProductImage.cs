using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Catalog;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string Url { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
