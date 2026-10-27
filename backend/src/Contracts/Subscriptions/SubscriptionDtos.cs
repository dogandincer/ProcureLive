namespace ProcureLive.Contracts.Subscriptions;

public sealed record SeatBandPriceDto(string Band, int MinUsers, int? MaxUsers, decimal PriceUsd);

public sealed record SubscriptionPlansDto(IReadOnlyList<SeatBandPriceDto> Bands, int DurationMonths, int StandardJoinLimit);

public sealed record MySubscriptionDto(
    Guid CompanyId,
    string CompanyName,
    string Plan,
    bool IsPlus,
    string? SeatBand,
    decimal? PriceUsd,
    DateTime? ExpiresAt,
    int JoinedCount,
    int QuotaLimit,
    int? RemainingJoins,
    int UserCount);

/// <summary>Kart için yalnızca son 4 hane + kart sahibi gönderilir (demo/mock ödeme).</summary>
public sealed record PurchaseSubscriptionRequest(string SeatBand, string CardHolder, string CardLast4);

public sealed record PurchaseResultDto(
    bool Succeeded, string? PaymentReference, string? FailureReason, string? SeatBand, decimal? PriceUsd, DateTime? ExpiresAt);
