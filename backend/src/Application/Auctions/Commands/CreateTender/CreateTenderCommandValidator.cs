using FluentValidation;

namespace ProcureLive.Application.Auctions.Commands.CreateTender;

public sealed class CreateTenderCommandValidator : AbstractValidator<CreateTenderCommand>
{
    public CreateTenderCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250).WithMessage("İhale adı zorunludur ve en fazla 250 karakter olabilir.");
        RuleFor(x => x.ReferenceCode).MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.OwnerCompanyId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();

        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt).WithMessage("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");

        RuleFor(x => x.ExtensionTriggerMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExtensionDurationMinutes).GreaterThan(0).When(x => x.ExtensionDurationMinutes.HasValue)
            .WithMessage("Süre uzatma miktarı 0'dan büyük olmalıdır.");
        RuleFor(x => x.MaxExtensionMinutes).GreaterThanOrEqualTo(x => x.ExtensionDurationMinutes ?? 0)
            .When(x => x.MaxExtensionMinutes.HasValue)
            .WithMessage("Maksimum uzatma, tek seferlik uzatma süresinden küçük olamaz.");
        // Sniper açıksa (tetik > 0) uzatma miktarı tanımlı olmalı
        RuleFor(x => x.ExtensionDurationMinutes).NotNull().When(x => x.ExtensionTriggerMinutes > 0)
            .WithMessage("Süre uzatma aktifken uzatma miktarı (dakika) belirtilmelidir.");

        RuleFor(x => x.Lines).NotEmpty().WithMessage("En az bir kalem eklenmelidir.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(1000).WithMessage("Kalem açıklaması zorunludur.");
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır.");
            line.RuleFor(l => l.Specification).MaximumLength(4000);
        });

        RuleFor(x => x.VendorIds).NotEmpty().WithMessage("En az bir tedarikçi (cari) seçilmelidir.");

        RuleForEach(x => x.Attachments).ChildRules(att =>
        {
            att.RuleFor(a => a.FileName).NotEmpty().MaximumLength(300);
            att.RuleFor(a => a.Url).NotEmpty().MaximumLength(1000);
        });
    }
}
