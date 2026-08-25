using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Identity;

/// <summary>Fine-grained permission catalog; Phase 1 seeds none, roles are enough, but the table is ready.</summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
