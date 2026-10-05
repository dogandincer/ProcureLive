using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Companies;

/// <summary>Many-to-many join: which industry sectors a supplier company operates in.</summary>
public class CompanySector : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid SectorId { get; set; }

    public Sector Sector { get; set; } = null!;
}
