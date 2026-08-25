using ProcureLive.Domain.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Companies;

public class Address : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public AddressType AddressType { get; set; }

    public string Country { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string? District { get; set; }

    public string? PostalCode { get; set; }

    public string Line1 { get; set; } = string.Empty;

    public string? Line2 { get; set; }
}
