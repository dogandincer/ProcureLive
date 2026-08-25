using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Identity;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
