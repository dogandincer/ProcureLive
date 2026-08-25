using FluentValidation;

namespace ProcureLive.Application.Rfq.Commands.RespondToRfqInvitation;

public sealed class RespondToRfqInvitationCommandValidator : AbstractValidator<RespondToRfqInvitationCommand>
{
    public RespondToRfqInvitationCommandValidator()
    {
        RuleFor(x => x.RfqProcessId).NotEmpty();
        RuleFor(x => x.DeclineReason).NotEmpty().When(x => !x.Accept)
            .WithMessage("Reddetme gerekçesi zorunludur.");
    }
}
