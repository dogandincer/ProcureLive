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
    DateTime CreatedAt);

public sealed record AuctionDetailDto(
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
    decimal? MinDecrementAmount,
    decimal? MinDecrementPercentage,
    int ExtensionRuleMinutes,
    IReadOnlyCollection<AuctionItemDto> Items,
    IReadOnlyCollection<AuctionInvitationDto> Invitations);

public sealed record AuctionItemDto(
    Guid Id,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? TargetPrice,
    int SortOrder,
    decimal? CurrentBestAmount,
    int BidCount);

public sealed record AuctionInvitationDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Status,
    DateTime InvitedAt,
    DateTime? RespondedAt);

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
    string Status);

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
    int ExtensionRuleMinutes);

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
