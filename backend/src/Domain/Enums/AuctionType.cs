namespace ProcureLive.Domain.Enums;

/// <summary>
/// English: fiyat yukarı doğru artan klasik açık artırma.
/// Dutch: fiyat yukarıdan aşağıya inen (Hollanda usulü) açık artırma.
/// ReverseSealed: tedarikçilerin fiyatı aşağı çektiği, kapalı zarf ters mühürlü ihale.
/// SealedBid: tek seferlik kapalı teklif.
/// </summary>
public enum AuctionType
{
    English = 0,
    Dutch = 1,
    ReverseSealed = 2,
    SealedBid = 3
}
