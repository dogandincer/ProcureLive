using ProcureLive.Domain.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Erp;

/// <summary>
/// UyumERP SOAP bağlantısı (tek satır, yalnızca Admin). ERP entegrasyonu premium özelliktir: sunucu yapılandırmasında
/// Features:ErpIntegration açık değilse bu ayar hiç kullanılmaz. Şifre Data Protection ile şifreli saklanır, API'den dönmez.
/// </summary>
public class ErpSettings : AuditableEntity
{
    public ErpMode Mode { get; set; } = ErpMode.Off;

    /// <summary>UyumGetWebService.asmx adresi (okuma: GetDataTableWithSQL).</summary>
    public string GetServiceUrl { get; set; } = string.Empty;

    /// <summary>UyumSaveWebService.asmx adresi (yazma: SaveOffer, SaveOrderM, SaveEntity, SaveItem, SaveUyumObjectFromXML).</summary>
    public string SaveServiceUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    public string? PasswordProtected { get; set; }

    /// <summary>Belgelerin açılacağı ERP şirket ve şube kodu.</summary>
    public string CoCode { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;

    /// <summary>Satınalma teklifi ve siparişinin ERP hareket (belge) türü kodu.</summary>
    public string OfferDocTraCode { get; set; } = string.Empty;
    public string OrderDocTraCode { get; set; } = string.Empty;

    /// <summary>ERP'de açılan belgelerin numara öneki (ör. "PL-").</summary>
    public string DocNoPrefix { get; set; } = "PL-";

    /// <summary>Cari aktarımında yalnızca bu önekle başlayan ERP carileri listelenir (ör. satıcılar için "320"); boşsa hepsi.</summary>
    public string? EntityCodePrefix { get; set; }

    /// <summary>Onaylanan (aktif) yeni tedarikçiler ERP'ye cari olarak otomatik gönderilsin mi.</summary>
    public bool ExportNewSuppliers { get; set; }

    /// <summary>ERP kodu olmayan ürünler ERP'ye stok kartı olarak otomatik gönderilsin mi.</summary>
    public bool ExportNewProducts { get; set; }
}
