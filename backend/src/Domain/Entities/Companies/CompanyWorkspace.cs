using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Workspaces;

namespace ProcureLive.Domain.Entities.Companies;

/// <summary>
/// Tedarikçi carinin göründüğü (onaylı olduğu) işyerleri. Company.WorkspaceId birincil işyeridir; bu tablo
/// ek işyerlerini tutar. Bir cari, ihalenin işyerinde görünüyorsa o ihale için "onaylı cari"dir.
/// </summary>
public class CompanyWorkspace : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid WorkspaceId { get; set; }

    public Workspace Workspace { get; set; } = null!;
}
