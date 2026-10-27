namespace ProcureLive.Application.Common.Interfaces;

/// <summary>Yalnızca son 4 hane ve kart sahibi taşınır — tam kart numarası backend'e hiç gelmez.</summary>
public sealed record PaymentCardInfo(string CardHolder, string CardLast4);

public sealed record PaymentResult(bool Succeeded, string? Reference, string? FailureReason);

/// <summary>Ödeme sağlayıcısı soyutlaması. Şimdilik mock; gerçek sağlayıcı (Stripe vb.) yalnızca Infrastructure'ı değiştirir.</summary>
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(decimal amountUsd, string description, PaymentCardInfo card, CancellationToken cancellationToken);
}
