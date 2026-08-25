using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Ai;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Infrastructure.Services;

/// <summary>
/// Phase 4 placeholder for IAuctionModelRecommender: simple, explainable rules over the prepared
/// feature vector — NOT a trained model. It exists so the API/UI contract is usable today; replace
/// this class with a real ML.NET model or an external scoring service call when one is trained on
/// enough historical auctions ("binlerce açık artırma analizi" — not yet available).
/// </summary>
public sealed class HeuristicAuctionModelRecommender : IAuctionModelRecommender
{
    public Task<(string RecommendedAuctionType, double ConfidenceScore, string Rationale)> RecommendAsync(
        AuctionModelFeatureSetDto features,
        CancellationToken cancellationToken)
    {
        if (features.HistoricalAuctionCount < 5)
        {
            return Task.FromResult((
                AuctionType.English.ToString(),
                0.3,
                "Yeterli geçmiş ihale verisi yok (5'ten az); güvenli varsayılan olarak klasik İngiliz usulü (artan fiyat) öneriliyor. Daha fazla ihale tamamlandıkça öneri güvenilirliği artacaktır."));
        }

        if (features.EstimatedParticipantCount <= 2)
        {
            return Task.FromResult((
                AuctionType.SealedBid.ToString(),
                0.55,
                $"Tahmini katılımcı sayısı düşük ({features.EstimatedParticipantCount}); az katılımcıda rekabeti en üst düzeye çıkarmak için kapalı zarf (Sealed Bid) usulü öneriliyor."));
        }

        if (features.AverageExtensionCount >= 2 && features.AverageUniqueBidderCount >= 3)
        {
            return Task.FromResult((
                AuctionType.English.ToString(),
                0.7,
                $"Benzer ihalelerde ortalama {features.AverageUniqueBidderCount:F1} benzersiz teklif veren ve {features.AverageExtensionCount:F1} otomatik süre uzatımı gözlendi — yüksek rekabet İngiliz usulü açık artırmayı destekliyor."));
        }

        if (features.AveragePriceChangePercentage.HasValue && features.AveragePriceChangePercentage.Value < 5)
        {
            return Task.FromResult((
                AuctionType.ReverseSealed.ToString(),
                0.5,
                $"Benzer ihalelerde ortalama fiyat değişimi düşük (%{features.AveragePriceChangePercentage.Value:F1}); fiyat baskısını artırmak için Ters Mühürlü usul öneriliyor."));
        }

        return Task.FromResult((
            AuctionType.English.ToString(),
            0.45,
            "Belirgin bir sinyal bulunamadı; en yaygın ve anlaşılır seçenek olan İngiliz usulü açık artırma varsayılan olarak öneriliyor."));
    }
}
