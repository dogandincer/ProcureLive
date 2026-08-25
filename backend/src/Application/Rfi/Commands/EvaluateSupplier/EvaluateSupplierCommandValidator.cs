using FluentValidation;

namespace ProcureLive.Application.Rfi.Commands.EvaluateSupplier;

public sealed class EvaluateSupplierCommandValidator : AbstractValidator<EvaluateSupplierCommand>
{
    public EvaluateSupplierCommandValidator()
    {
        RuleFor(x => x.RfiProcessId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.TotalScore).GreaterThanOrEqualTo(0);
    }
}
