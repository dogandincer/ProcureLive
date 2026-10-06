using FluentValidation;

namespace ProcureLive.Application.Companies.Commands.EvaluateCompany;

public sealed class EvaluateCompanyCommandValidator : AbstractValidator<EvaluateCompanyCommand>
{
    public EvaluateCompanyCommandValidator()
    {
        RuleFor(x => x.QualityScore).InclusiveBetween(1, 5).WithMessage("Kalite puanı 1 ile 5 arasında olmalıdır.");
        RuleFor(x => x.DeliveryScore).InclusiveBetween(1, 5).WithMessage("Teslimat puanı 1 ile 5 arasında olmalıdır.");
        RuleFor(x => x.CommunicationScore).InclusiveBetween(1, 5).WithMessage("İletişim puanı 1 ile 5 arasında olmalıdır.");
        RuleFor(x => x.PriceScore).InclusiveBetween(1, 5).WithMessage("Fiyat puanı 1 ile 5 arasında olmalıdır.");
        RuleFor(x => x.ComplianceScore).InclusiveBetween(1, 5).WithMessage("Uyum puanı 1 ile 5 arasında olmalıdır.");
        RuleFor(x => x.Comments).MaximumLength(2000).WithMessage("Yorum en fazla 2000 karakter olabilir.");
    }
}
