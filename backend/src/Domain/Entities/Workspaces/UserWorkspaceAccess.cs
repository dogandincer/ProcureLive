using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Workspaces;

/// <summary>
/// Satınalmacıya Admin tarafından verilen işyeri yetkisi. Firma yetkisi buradan türetilir (işyerinin firması);
/// Admin'in kaydı olmaz — tüm firma ve işyerlerine yetkilidir.
/// </summary>
public class UserWorkspaceAccess : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid WorkspaceId { get; set; }

    public Workspace Workspace { get; set; } = null!;

    public DateTime GrantedAt { get; set; }

    public Guid? GrantedByUserId { get; set; }
}
