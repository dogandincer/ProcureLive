using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Domain.Entities.Companies;

/// <summary>Many-to-many join: which catalog categories a supplier company operates in.</summary>
public class CompanySupplierCategory : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;
}
