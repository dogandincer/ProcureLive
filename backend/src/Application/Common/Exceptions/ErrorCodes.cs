namespace ProcureLive.Application.Common.Exceptions;

/// <summary>
/// Hata yanıtındaki "code" alanı. Mesajlar dile göre çevrildiği için istemci davranışı
/// (Plus'a yönlendirme, giriş ekranına geçiş vb.) metne değil bu koda bakmalıdır.
/// </summary>
public static class ErrorCodes
{
    public const string QuotaExceeded = "QuotaExceeded";
    public const string PlusRequired = "PlusRequired";
    public const string InviteInvalid = "InviteInvalid";
    public const string InviteExpired = "InviteExpired";
    public const string InviteUsed = "InviteUsed";
    public const string InviteCompanyMismatch = "InviteCompanyMismatch";
    public const string InviteEmailMismatch = "InviteEmailMismatch";
    public const string AccountExists = "AccountExists";
    public const string AuctionClosed = "AuctionClosed";
    public const string InvalidStatusTransition = "InvalidStatusTransition";
    public const string AccountLocked = "AccountLocked";
    public const string ApprovalRequired = "ApprovalRequired";
    public const string RequestPending = "RequestPending";
    public const string ProcessClosed = "ProcessClosed";
    public const string RequiredAnswersMissing = "RequiredAnswersMissing";
    public const string InvalidCredentials = "InvalidCredentials";
    public const string AccountInactive = "AccountInactive";
}
