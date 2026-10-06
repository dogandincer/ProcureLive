using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Finance;
using ProcureLive.Domain.Entities.Workspaces;
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

    /// <summary>URL of an interactive 3D model (.glb/.gltf) for this product, if one has been set.</summary>
    public string? Model3dUrl { get; set; }

    /// <summary>How the 3D model was produced: "Uploaded" (direct URL) or "AiGenerated" (from product photos).</summary>
    public string? Model3dSource { get; set; }

    public Guid? WorkspaceId { get; set; }

    public Workspace? Workspace { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
}
