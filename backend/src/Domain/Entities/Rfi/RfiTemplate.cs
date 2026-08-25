using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Domain.Entities.Rfi;

public class RfiTemplate : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RfiQuestion> Questions { get; set; } = new List<RfiQuestion>();
}
