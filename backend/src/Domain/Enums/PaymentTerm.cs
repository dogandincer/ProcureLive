namespace ProcureLive.Domain.Enums;

/// <summary>Ödeme vadesi seçenekleri (ihale teklif şartları).</summary>
public enum PaymentTerm
{
    Advance = 0,
    Net15 = 1,
    Net30 = 2,
    Net60 = 3,
    Net90 = 4
}
