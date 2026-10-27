using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Entities.Workspaces;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Companies;

public class Company : AuditableEntity, ISoftDelete
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Resmi ticari unvan (e.g. "ACME Tedarik Sanayi ve Ticaret A.Ş.").</summary>
    public string? TradeName { get; set; }

    public string TaxNumber { get; set; } = string.Empty;

    public string TaxOffice { get; set; } = string.Empty;

    /// <summary>MERSİS numarası (16 haneli).</summary>
    public string? MersisNo { get; set; }

    public CompanyType CompanyType { get; set; }

    public CompanyStatus Status { get; set; } = CompanyStatus.PendingApproval;

    public string? Website { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public int? FoundationYear { get; set; }

    public int? EmployeeCount { get; set; }

    public string? Description { get; set; }

    /// <summary>Logo görsel adresi (URL-tabanlı, ürün görselleri gibi).</summary>
    public string? LogoUrl { get; set; }

    /// <summary>KVKK: false ise e-posta/telefon diğer firmalara maskeli gösterilir (varsayılan kapalı).</summary>
    public bool ShowContactInfo { get; set; }

    /// <summary>Firmanın açtığı ihaleler Plus aramalarında listelenebilir mi (ihale bazında ayrıca işaretlenmeli).</summary>
    public bool ListTendersForPlus { get; set; }

    public Guid? WorkspaceId { get; set; }

    public Workspace? Workspace { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<ContactPerson> ContactPersons { get; set; } = new List<ContactPerson>();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    public ICollection<CompanySupplierCategory> SupplierCategories { get; set; } = new List<CompanySupplierCategory>();

    public ICollection<CompanySector> Sectors { get; set; } = new List<CompanySector>();

    public ICollection<CompanyEvaluation> Evaluations { get; set; } = new List<CompanyEvaluation>();
}
