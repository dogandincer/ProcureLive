using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Security;

/// <summary>
/// RFI/RFQ katılım kuralları (Standart / Plus). İhale kotası ile aynı <see cref="SupplierAccessContext"/> kullanılır:
///   - Standart cari ihale kotasını doldurduysa henüz kabul etmediği RFI/RFQ davetleri kısıtlı (QuotaExceeded) görünür ve
///     kabul edilemez — arayüz "Plus alınız" ile paket ekranına yönlendirir. Kabul edilmiş süreçler etkilenmez.
///   - Gönderilmiş yanıt/teklif yalnızca Plus üyelerce ve süreç açıkken (Gönderildi / Devam Ediyor) revize edilebilir.
/// </summary>
public static class ProcessParticipationRules
{
    public const string QuotaMessage =
        "Standart üyelik katılım kotanız doldu. Yeni RFI/RFQ davetlerini kabul etmek için Plus'a geçin.";

    public const string RevisionMessage = "Gönderdiğiniz yanıtı revize etmek Plus üyelere açıktır.";

    public const string ClosedMessage = "Bu süreç yanıtlara kapalı.";

    public static bool IsOpen(ProcessStatus status) => status is ProcessStatus.Sent or ProcessStatus.InProgress;

    public static bool IsAccepted(InvitationStatus? status) =>
        status is InvitationStatus.Accepted or InvitationStatus.Participating;

    public static AuctionAccessLevel Evaluate(SupplierAccessContext access, InvitationStatus? myStatus) =>
        myStatus == InvitationStatus.Invited && access.QuotaReached ? AuctionAccessLevel.QuotaExceeded : AuctionAccessLevel.Full;

    /// <summary>İlk yanıt her üyeye açık; gönderilmiş yanıtın revizyonu yalnızca Plus'a.</summary>
    public static bool CanSubmit(SupplierAccessContext access, ProcessStatus status, InvitationStatus? myStatus, bool hasSubmitted) =>
        IsOpen(status) && IsAccepted(myStatus) && (!hasSubmitted || access.IsPlus);
}
