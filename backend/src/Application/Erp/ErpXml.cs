using System.Globalization;
using System.Security;

namespace ProcureLive.Application.Erp;

/// <summary>
/// UyumSaveWebService isteklerinin Value gövdeleri (WSDL: EntityDef, ItemCardDef, OfferDef, OrderDef) ve teklif revizyonu için
/// SaveUyumObjectFromXML ile gönderilen PSM.OfferM nesnesi (OfferMaster'daki yapının aynısı). Sayılar ERP'nin beklediği
/// biçimde: SOAP alanlarında nokta, Uyum nesne XML'inde virgül ondalık ayırıcı.
/// </summary>
public static class ErpXml
{
    public sealed record Line(int LineNo, string ItemCode, string UnitCode, decimal Qty, decimal UnitPrice, string CurCode, DateTime? DeliveryDate, string? Note);

    public sealed record RevisionLine(int OfferDId, decimal UnitPrice, string CurCode, decimal Rate, DateTime? DeliveryDate, string Note);

    public static string Entity(string coCode, string entityCode, string name, string? taxNo, string? taxOffice, string? phone, string? email, string? web) =>
        Tag("CoCode", coCode) + Tag("EntityCode", entityCode) + Tag("EntityName", name)
        + "<tem:SearchByEntityCode>true</tem:SearchByEntityCode><tem:PutByEntityCode>true</tem:PutByEntityCode>"
        + Tag("TaxNo", taxNo) + Tag("TaxOfficeName", taxOffice) + Tag("Tel", phone) + Tag("Email", email) + Tag("WebSite", web);

    public static string Item(string coCode, string branchCode, string itemCode, string name, string unitCode, string? brand) =>
        Tag("CoCode", coCode) + Tag("BranchCode", branchCode) + Tag("ItemCode", itemCode) + Tag("ItemName", name)
        + Tag("UnitCode", unitCode) + Tag("BrandCode", brand);

    /// <summary>OfferDef / OrderDef ortak başlık + satırlar. Belgenin satınalma belgesi olduğu hareket türü kodundan (DocTraCode) anlaşılır.</summary>
    public static string Document(
        bool order, string coCode, string branchCode, string docTraCode, string docNo, DateTime docDate,
        string entityCode, string curCode, string? note1, string? note2, IEnumerable<Line> lines)
    {
        string detailTag = order ? "OrderDetailDef" : "OfferDetailDef";
        string details = string.Concat(lines.Select(l =>
            $"<tem:{detailTag}>"
            + $"<tem:LineNo>{l.LineNo}</tem:LineNo><tem:LineType>S</tem:LineType>"
            + Tag("ItemCode", l.ItemCode) + Tag("DcardCode", l.ItemCode) + Tag("UnitCode", l.UnitCode)
            + $"<tem:Qty>{Dot(l.Qty)}</tem:Qty><tem:UnitPriceTra>{Dot(l.UnitPrice)}</tem:UnitPriceTra>"
            + Tag("CurCode", l.CurCode) + Tag("Note1", l.Note)
            + (order && l.DeliveryDate is { } dd ? $"<tem:DeliveryDate>{dd:yyyy-MM-dd}T00:00:00</tem:DeliveryDate>" : "")
            + $"</tem:{detailTag}>"));

        return Tag("CoCode", coCode) + Tag("BranchCode", branchCode) + Tag("DocTraCode", docTraCode) + Tag("DocNo", docNo)
            + $"<tem:DocDate>{docDate:yyyy-MM-dd}T00:00:00</tem:DocDate>"
            + Tag("EntityCode", entityCode) + Tag("CurCode", curCode) + Tag("Note1", note1) + Tag("Note2", note2)
            + (order ? "<tem:OrderStatus>Açık</tem:OrderStatus>" : "")
            + $"<tem:Details>{details}</tem:Details>";
    }

    /// <summary>Teklif revizyonu: mevcut ERP teklifinin satır fiyatlarını günceller (SaveUyumObjectFromXML, CDATA içinde Uyum nesnesi).</summary>
    public static string OfferRevision(int offerMId, IEnumerable<RevisionLine> lines)
    {
        string details = string.Concat(lines.Select(l =>
            "<UyumDetailItem DetailProperty=\"OfferDCollection\">"
            + $"<Id>{l.OfferDId}</Id>"
            + $"<UnitPriceTra>{Comma(l.UnitPrice)}</UnitPriceTra>"
            + $"<UnitPrice>{Comma(l.UnitPrice)}</UnitPrice>"
            + $"<CurTraCode>{X(l.CurCode)}</CurTraCode>"
            + $"<CurRateTra>{Comma(l.Rate)}</CurRateTra>"
            + (l.DeliveryDate is { } dd ? $"<DeliveryDate>{dd:yyyy-MM-dd}T00:00:00</DeliveryDate>" : "")
            + $"<Note3>{X(l.Note)}</Note3>"
            + "</UyumDetailItem>"));

        return $"<![CDATA[<Uyum Type=\"PSM.OfferM, PSM\" LoadDetail='true' PromptValues='10505'><Id>{offerMId}</Id>{details}</Uyum>]]>";
    }

    /// <summary>SQL metin sabiti: tek tırnak kaçışlanır (yalnızca okuma sorgularında, kullanıcı aramasıyla).</summary>
    public static string Sql(string? value) => "'" + (value ?? "").Replace("'", "''") + "'";

    public static string X(string? value) => SecurityElement.Escape(value ?? "") ?? "";

    private static string Tag(string name, string? value) => $"<tem:{name}>{X(value)}</tem:{name}>";

    private static string Dot(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string Comma(decimal value) => Dot(value).Replace('.', ',');
}
