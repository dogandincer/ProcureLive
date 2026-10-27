using Microsoft.Extensions.Logging;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services;

/// <summary>
/// Demo ödeme sağlayıcısı (Stripe vb. bağlı değil). Her ödemeyi başarılı sayar; son 4 hanesi "0002"
/// olan kart reddedilir (red akışını göstermek için). Gerçek sağlayıcı geldiğinde yalnızca bu sınıf değişir.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<MockPaymentGateway> _logger;

    public MockPaymentGateway(ILogger<MockPaymentGateway> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentResult> ChargeAsync(decimal amountUsd, string description, PaymentCardInfo card, CancellationToken cancellationToken)
    {
        await Task.Delay(600, cancellationToken); // gerçekçi bir sağlayıcı gecikmesi

        if (card.CardLast4 == "0002")
        {
            _logger.LogInformation("[ÖDEME MOCK] Reddedildi: {Amount} USD, {Description}", amountUsd, description);
            return new PaymentResult(false, null, "Kart reddedildi (demo red kartı).");
        }

        string reference = $"MOCK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        _logger.LogInformation("[ÖDEME MOCK] Başarılı: {Amount} USD, {Description}, ref {Reference}", amountUsd, description, reference);
        return new PaymentResult(true, reference, null);
    }
}
