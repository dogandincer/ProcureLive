using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Companies;

public class Company : AuditableEntity, ISoftDelete
{
    public string Name { get; set; } = string.Empty;

    public string TaxNumber { get; set; } = string.Empty;

    public string TaxOffice { get; set; } = string.Empty;

    public CompanyType CompanyType { get; set; }

    public CompanyStatus Status { get; set; } = CompanyStatus.PendingApproval;

    public string? Website { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<ContactPerson> ContactPersons { get; set; } = new List<ContactPerson>();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    public ICollection<CompanySupplierCategory> SupplierCategories { get; set; } = new List<CompanySupplierCategory>();

    public ICollection<CompanySector> Sectors { get; set; } = new List<CompanySector>();
}
