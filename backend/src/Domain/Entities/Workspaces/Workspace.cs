using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Workspaces;

/// <summary>Represents a business unit / işyeri that companies and products are scoped to.</summary>
public class Workspace : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
