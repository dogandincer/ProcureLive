using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Contracts.Subscriptions;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Subscriptions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Subscriptions.Commands;

/// <summary>
/// Plus satın alımı (demo). Ödeme <see cref="IPaymentGateway"/> üzerinden alınır — şimdilik mock; gerçek
/// sağlayıcıya geçişte yalnızca Infrastructure değişir. Reddedilen ödeme hata değil, Succeeded=false sonuçtur.
/// </summary>
public sealed record PurchaseSubscriptionCommand(SeatBand SeatBand, string CardHolder, string CardLast4) : IRequest<PurchaseResultDto>;

public sealed class PurchaseSubscriptionCommandValidator : AbstractValidator<PurchaseSubscriptionCommand>
{
    public PurchaseSubscriptionCommandValidator()
    {
        RuleFor(x => x.SeatBand).IsInEnum();
        RuleFor(x => x.CardHolder).NotEmpty().MaximumLength(100).WithMessage("Kart üzerindeki isim zorunludur.");
        RuleFor(x => x.CardLast4).Matches(@"^\d{4}$").WithMessage("Kartın son 4 hanesi 4 rakamdan oluşmalıdır.");
    }
}

public sealed class PurchaseSubscriptionCommandHandler : IRequestHandler<PurchaseSubscriptionCommand, PurchaseResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IIdentityService _identityService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly INotificationRealtimeService _notificationRealtimeService;
    private readonly IDateTime _dateTime;

    public PurchaseSubscriptionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISubscriptionService subscriptionService,
        IIdentityService identityService,
        IPaymentGateway paymentGateway,
        INotificationRealtimeService notificationRealtimeService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _subscriptionService = subscriptionService;
        _identityService = identityService;
        _paymentGateway = paymentGateway;
        _notificationRealtimeService = notificationRealtimeService;
        _dateTime = dateTime;
    }

    public async Task<PurchaseResultDto> Handle(PurchaseSubscriptionCommand request, CancellationToken cancellationToken)
    {
        Guid companyId = _currentUserService.CompanyId
            ?? throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");

        Company company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), companyId);

        SeatBandPrice band = SubscriptionPricing.For(request.SeatBand);

        // Paket, firmadaki mevcut kullanıcı sayısını karşılamalı (5-10 paketi 12 kullanıcılı firmaya satılamaz).
        int userCount = (await _identityService.ListUsersAsync(null, companyId, 1, 1, null, cancellationToken)).TotalCount;
        if (band.MaxUsers is int maxUsers && userCount > maxUsers)
        {
            throw new ConflictException(
                $"Firmanızda {userCount} kullanıcı var; bu paket en fazla {maxUsers} kullanıcı içindir. Daha geniş bir paket seçin.");
        }

        CompanySubscription? current = await _subscriptionService.GetActivePlusAsync(companyId, cancellationToken);
        if (current is not null && current.SeatBand == request.SeatBand)
        {
            throw new ConflictException("Bu paket zaten aktif.");
        }

        PaymentResult payment = await _paymentGateway.ChargeAsync(
            band.PriceUsd,
            $"ProcureLive Plus {band.Band} - {company.Name}",
            new PaymentCardInfo(request.CardHolder.Trim(), request.CardLast4),
            cancellationToken);

        if (!payment.Succeeded || payment.Reference is null)
        {
            return new PurchaseResultDto(false, null, payment.FailureReason, band.Band.ToString(), band.PriceUsd, null);
        }

        DateTime now = _dateTime.UtcNow;

        // Paket değişiminde eski kayıt geçmiş için korunur, yalnızca iptal edilir.
        if (current is not null)
        {
            current.Status = SubscriptionStatus.Cancelled;
        }

        var subscription = new CompanySubscription
        {
            CompanyId = companyId,
            Plan = SubscriptionPlan.Plus,
            SeatBand = band.Band,
            PriceUsd = band.PriceUsd,
            Status = SubscriptionStatus.Active,
            StartsAt = now,
            ExpiresAt = now.AddMonths(SubscriptionPricing.PlanDurationMonths),
            PaymentReference = payment.Reference,
            PurchasedByUserId = _currentUserService.UserId ?? Guid.Empty,
        };

        _context.CompanySubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        await NotificationCreator.CreateForInternalUsersAsync(
            _context, _notificationRealtimeService, _dateTime,
            NotificationType.System,
            "Plus Abonelik",
            $"\"{company.Name}\" Plus üyeliğe geçti.",
            nameof(Company), companyId, cancellationToken);

        return new PurchaseResultDto(
            true, subscription.PaymentReference, null, band.Band.ToString(), band.PriceUsd, subscription.ExpiresAt);
    }
}
