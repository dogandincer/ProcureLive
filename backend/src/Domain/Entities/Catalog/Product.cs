using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Finance;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Catalog;

public class Product : AuditableEntity, ISoftDelete
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public UnitOfMeasure UnitOfMeasure { get; set; }

    public string? Brand { get; set; }

    public Guid? DefaultCurrencyId { get; set; }

    public Currency? DefaultCurrency { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
}
