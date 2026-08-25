using FluentValidation;

namespace ProcureLive.Application.Rfi.Commands.RespondToRfiInvitation;

public sealed class RespondToRfiInvitationCommandValidator : AbstractValidator<RespondToRfiInvitationCommand>
{
    public RespondToRfiInvitationCommandValidator()
    {
        RuleFor(x => x.RfiProcessId).NotEmpty();
        RuleFor(x => x.DeclineReason).NotEmpty().When(x => !x.Accept)
            .WithMessage("Reddetme gerekçesi zorunludur.");
    }
}
