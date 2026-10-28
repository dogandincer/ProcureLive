namespace ProcureLive.Contracts.Auctions;

public sealed record AuctionDto(
    Guid Id,
    string Title,
    string? Description,
    string AuctionType,
    string Status,
    Guid OwnerCompanyId,
    string OwnerCompanyName,
    Guid CurrencyId,
    string CurrencyCode,
    DateTime StartAt,
    DateTime EndAt,
    DateTime? ActualStartAt,
    DateTime? ActualEndAt,
    decimal? ReservePrice,
    int ExtensionRuleMinutes,
    int ItemCount,
    int InvitationCount,
    DateTime CreatedAt,
    string AccessLevel = "Full",
    bool IsJoined = false,
    bool IsListedForPlus = false,
    string? MyInvitationStatus = null,
    bool RequiresApproval = false,
    string? ParticipationStatus = null,
    string? WorkspaceName = null);

public sealed record AuctionDetailDto(
    Guid Id,
    string Title,
    string? ReferenceCode,
    string? Description,
    string AuctionType,
    string Status,
    Guid OwnerCompanyId,
    string OwnerCompanyName,
    Guid CurrencyId,
    string CurrencyCode,
    DateTime StartAt,
    DateTime EndAt,
    DateTime? ActualStartAt,
    DateTime? ActualEndAt,
    decimal? ReservePrice,
    decimal? MinDecrementAmount,
    decimal? MinDecrementPercentage,
    int ExtensionRuleMinutes,
    int? ExtensionDurationMinutes,
    int? MaxExtensionMinutes,
    string? PaymentTerm,
    string? DeliveryTerm,
    bool AllowAlternativeProducts,
    bool AllowPartialBids,
    IReadOnlyCollection<AuctionItemDto> Items,
    IReadOnlyCollection<AuctionInvitationDto> Invitations,
    IReadOnlyCollection<AuctionAttachmentDto> Attachments,
    string AccessLevel = "Full",
    bool IsJoined = false,
    bool CanJoin = false,
    bool IsListedForPlus = false,
    int ItemCount = 0,
    bool RequiresApproval = false,
    ParticipationRequestDto? MyParticipationRequest = null,
    Guid? WorkspaceId = null,
    string? WorkspaceName = null,
    Guid? ResponsibleUserId = null,
    string? ResponsibleUserName = null);

public sealed record AuctionItemDto(
    Guid Id,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? TargetPrice,
    DateTime? RequestedDeliveryDate,
    string? Specification,
    int SortOrder,
    decimal? CurrentBestAmount,
    int BidCount,
    // Tedarikçiye özel: kendi en iyi birim fiyatı; açık ihalede sırası (1 = lider) ve teklif veren firma sayısı.
    decimal? MyBestAmount = null,
    int? MyRank = null,
    int? CompetitorCount = null);

public sealed record AuctionAttachmentDto(
    Guid Id,
    string FileName,
    string Url,
    string? ContentType,
    long SizeBytes);

public sealed record AuctionInvitationDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Status,
    DateTime InvitedAt,
    DateTime? RespondedAt,
    string Origin = "Direct");

public sealed record BidDto(
    Guid Id,
    Guid AuctionItemId,
    Guid CompanyId,
    string CompanyName,
    decimal Amount,
    Guid CurrencyId,
    DateTime BidTime,
    string Status,
    bool IsMine);

public sealed record BidComparisonDto(
    Guid BidId,
    Guid CompanyId,
    string CompanyName,
    decimal Amount,
    string CurrencyCode,
    decimal NormalizedAmountInBaseCurrency,
    DateTime BidTime,
    string Status,
    bool IsAwarded = false);

/// <summary>Canlı akışın açılışta doldurulan geçmişi. Tedarikçi rakibin adını hiç, kapalı zarfta tutarını da görmez.</summary>
public sealed record AuctionFeedEntryDto(
    Guid BidId,
    Guid AuctionItemId,
    string ItemDescription,
    string? CompanyName,
    decimal? Amount,
    string CurrencyCode,
    bool IsMine,
    DateTime BidTime);

public sealed record CreateAuctionRequest(
    string Title,
    string? Description,
    string AuctionType,
    Guid OwnerCompanyId,
    Guid CurrencyId,
    DateTime StartAt,
    DateTime EndAt,
    decimal? ReservePrice,
    decimal? MinDecrementAmount,
    decimal? MinDecrementPercentage,
    int ExtensionRuleMinutes,
    bool IsListedForPlus = false,
    Guid? WorkspaceId = null);

public sealed record AddAuctionItemRequest(
    Guid? ProductId,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? TargetPrice,
    int SortOrder);

public sealed record InviteCompaniesRequest(IReadOnlyCollection<Guid> CompanyIds);

public sealed record RespondToInvitationRequest(bool Accept, string? DeclineReason);

public sealed record ExtendAuctionRequest(DateTime NewEndAt, string Reason);

public sealed record PlaceBidRequest(decimal Amount, Guid CurrencyId);

public sealed record AwardAuctionItemRequest(Guid WinningBidId, string? Notes);
