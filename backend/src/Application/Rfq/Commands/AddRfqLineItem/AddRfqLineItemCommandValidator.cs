using FluentValidation;

namespace ProcureLive.Application.Rfq.Commands.AddRfqLineItem;

public sealed class AddRfqLineItemCommandValidator : AbstractValidator<AddRfqLineItemCommand>
{
    public AddRfqLineItemCommandValidator()
    {
        RuleFor(x => x.RfqProcessId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitOfMeasure).IsInEnum();
    }
}
