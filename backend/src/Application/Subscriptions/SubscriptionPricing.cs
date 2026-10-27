using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Subscriptions;

public sealed record SeatBandPrice(SeatBand Band, int MinUsers, int? MaxUsers, decimal PriceUsd);

/// <summary>Plus fiyat kataloğu (kullanıcı sayısı bazlı, 12 aylık).</summary>
public static class SubscriptionPricing
{
    public const int PlanDurationMonths = 12;

    public static readonly IReadOnlyList<SeatBandPrice> Bands = new[]
    {
        new SeatBandPrice(SeatBand.Seats5To10, 5, 10, 500m),
        new SeatBandPrice(SeatBand.Seats10To20, 10, 20, 1000m),
        new SeatBandPrice(SeatBand.Seats20Plus, 20, null, 2000m),
    };

    public static SeatBandPrice For(SeatBand band) => Bands.First(b => b.Band == band);
}
