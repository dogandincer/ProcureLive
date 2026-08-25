using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Catalog;

/// <summary>Simple EAV-style technical specification (e.g. "Voltage" -> "220V").</summary>
public class ProductSpecification : BaseEntity
{
    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
