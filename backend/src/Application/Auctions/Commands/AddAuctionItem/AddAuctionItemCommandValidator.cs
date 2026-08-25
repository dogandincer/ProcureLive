using FluentValidation;

namespace ProcureLive.Application.Auctions.Commands.AddAuctionItem;

public sealed class AddAuctionItemCommandValidator : AbstractValidator<AddAuctionItemCommand>
{
    public AddAuctionItemCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitOfMeasure).IsInEnum();
    }
}
