using FluentValidation;

namespace ProcureLive.Application.Rfq.Commands.InviteCompaniesToRfq;

public sealed class InviteCompaniesToRfqCommandValidator : AbstractValidator<InviteCompaniesToRfqCommand>
{
    public InviteCompaniesToRfqCommandValidator()
    {
        RuleFor(x => x.RfqProcessId).NotEmpty();
        RuleFor(x => x.CompanyIds).NotEmpty();
    }
}
