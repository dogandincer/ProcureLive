using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Erp;

/// <summary>
/// Bir ihaledeki bir tedarikçinin ERP teklifi (PSMT_OFFER_M). İhale yayınlanınca her davetli için açılır; tedarikçi teklif
/// verdikçe aynı ERP teklifi revize edilir. Satır eşlemesi: ihale kalemi → ERP teklif satırı (OFFER_D_ID), JSON.
/// </summary>
public class ErpOfferLink : BaseEntity
{
    public Guid AuctionId { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>ERP'deki belge numarası; ERP kimliği bununla geri okunur (SaveOffer kimlik döndürmez).</summary>
    public string DocNo { get; set; } = string.Empty;

    public int? ErpOfferMId { get; set; }

    /// <summary>{"auctionItemId": offerDId} — canlıda teklif açıldıktan sonra ERP'den okunur.</summary>
    public string? LineMapJson { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>ERP'ye en son yansıtılan teklif anı; daha yeni teklif varsa revizyon kuyruğa alınır.</summary>
    public DateTime? LastRevisedBidAt { get; set; }
}
