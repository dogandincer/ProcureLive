namespace ProcureLive.Contracts.Auctions;

/// <summary>Elle durum değişikliği. NewStartAt yalnızca yayınlarken, NewEndAt başlatma/devam/yeniden açmada kullanılır.</summary>
public sealed record ChangeAuctionStatusRequest(string Status, string? Reason, DateTime? NewStartAt, DateTime? NewEndAt);

public sealed record StatusTransitionOptionDto(
    string To, bool RequiresReason, bool RequiresEndAt, bool AllowsEndAt, bool AllowsStartAt);

public sealed record AuctionStatusChangeDto(
    Guid Id,
    string FromStatus,
    string ToStatus,
    string? Reason,
    Guid? ChangedByUserId,
    string? ChangedByName,
    bool IsAutomatic,
    DateTime ChangedAt);

public sealed record AuctionStatusPanelDto(
    Guid AuctionId,
    string Status,
    DateTime StartAt,
    DateTime EndAt,
    DateTime? ActualStartAt,
    DateTime? ActualEndAt,
    DateTime? PausedAt,
    bool HasAwards,
    IReadOnlyList<StatusTransitionOptionDto> Transitions,
    IReadOnlyList<AuctionStatusChangeDto> History,
    // Yayın onayı: firma onay istiyor ve kullanıcı satınalmacı → "Yayınla" yerine "Onaya gönder"; Admin bekleyen talebi onaylar/reddeder.
    bool PublishApprovalRequired = false,
    DateTime? ApprovalRequestedAt = null,
    string? ApprovalRequestedByName = null,
    string? ApprovalRejectReason = null);

public sealed record RejectPublishRequest(string Reason);

public sealed record RequestPublishApprovalRequest(DateTime? NewStartAt, DateTime? NewEndAt);

public sealed record PendingPublishApprovalDto(
    Guid AuctionId, string Title, string OwnerCompanyName, DateTime StartAt, DateTime RequestedAt, string? RequestedByName);
