using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Companies;

public class Sector : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CompanySector> Companies { get; set; } = new List<CompanySector>();
}
