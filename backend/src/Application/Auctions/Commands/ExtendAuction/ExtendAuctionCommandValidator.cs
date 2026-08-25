using FluentValidation;

namespace ProcureLive.Application.Auctions.Commands.ExtendAuction;

public sealed class ExtendAuctionCommandValidator : AbstractValidator<ExtendAuctionCommand>
{
    public ExtendAuctionCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
