using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Domain.Entities.Workspaces;

/// <summary>Represents a business unit / işyeri that companies and products are scoped to.</summary>
public class Workspace : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Bağlı olduğu Firma (Internal cari). Uygulama tarafında zorunludur; veritabanında boş olabilir ki eski
    /// kayıtlar başlangıçta FirmBootstrapper tarafından varsayılan firmaya bağlansın.
    /// </summary>
    public Guid? FirmId { get; set; }

    public Company? Firm { get; set; }

    /// <summary>Firmanın varsayılan işyeri (firma başına bir tane).</summary>
    public bool IsDefault { get; set; }
}
