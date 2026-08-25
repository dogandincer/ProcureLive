using FluentValidation;

namespace ProcureLive.Application.Auctions.Commands.RespondToInvitation;

public sealed class RespondToInvitationCommandValidator : AbstractValidator<RespondToInvitationCommand>
{
    public RespondToInvitationCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.DeclineReason).NotEmpty().When(x => !x.Accept)
            .WithMessage("Reddetme gerekçesi zorunludur.");
    }
}
