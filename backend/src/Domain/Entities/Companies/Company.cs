using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Entities.Workspaces;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Companies;

/// <summary>
/// Cari kart. CompanyType = Internal olan kayıtlar "Firma"dır (işyerlerinin üst ebeveyni, ihale/RFI/RFQ sahibi);
/// Supplier olanlar tedarikçi carilerdir. Firma'ya özgü alanlar (kısa ad, varsayılan, başlık eki) yalnızca Internal'da anlamlıdır.
/// </summary>
public class Company : AuditableEntity, ISoftDelete
{
    /// <summary>Firma / Cari kodu (benzersiz). Firmalarda zorunlu.</summary>
    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Firma kısa adı — doluysa uygulama başlığında kullanılır ("{Kısa Ad} e-İhale Yönetim Sistemi").</summary>
    public string? ShortName { get; set; }

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

    public string? Phone2 { get; set; }

    public string? MobilePhone { get; set; }

    public string? Fax { get; set; }

    public string? Email { get; set; }

    public string? Email2 { get; set; }

    /// <summary>Varsayılan firma (yalnızca bir tane). Giriş ekranı/sekme markası ve ilk seçim buradan gelir.</summary>
    public bool IsDefaultFirm { get; set; }

    /// <summary>Uygulama adı eki; boşsa "e-İhale Yönetim Sistemi" kullanılır (yalnızca varsayılan firmada etkili).</summary>
    public string? AppTitleSuffix { get; set; }

    public int? FoundationYear { get; set; }

    public int? EmployeeCount { get; set; }

    public string? Description { get; set; }

    /// <summary>Logo görsel adresi (URL-tabanlı, ürün görselleri gibi).</summary>
    public string? LogoUrl { get; set; }

    /// <summary>KVKK: false ise e-posta/telefon diğer firmalara maskeli gösterilir (varsayılan kapalı).</summary>
    public bool ShowContactInfo { get; set; }

    /// <summary>Firmanın açtığı ihaleler Plus aramalarında listelenebilir mi (ihale bazında ayrıca işaretlenmeli).</summary>
    public bool ListTendersForPlus { get; set; }

    /// <summary>Firma: satınalmacının yayınladığı ihale Admin onayından sonra planlanır (varsayılan açık).</summary>
    public bool RequirePublishApproval { get; set; } = true;

    /// <summary>ERP (UyumERP) cari kodu ve kimliği — premium ERP entegrasyonu açıksa dolar.</summary>
    public string? ErpEntityCode { get; set; }
    public int? ErpEntityId { get; set; }

    public Guid? WorkspaceId { get; set; }

    public Workspace? Workspace { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<ContactPerson> ContactPersons { get; set; } = new List<ContactPerson>();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    public ICollection<CompanySupplierCategory> SupplierCategories { get; set; } = new List<CompanySupplierCategory>();

    public ICollection<CompanySector> Sectors { get; set; } = new List<CompanySector>();

    public ICollection<CompanyEvaluation> Evaluations { get; set; } = new List<CompanyEvaluation>();

    /// <summary>Birincil işyeri dışında görünür olduğu (onaylı olduğu) işyerleri.</summary>
    public ICollection<CompanyWorkspace> VisibleWorkspaces { get; set; } = new List<CompanyWorkspace>();
}
