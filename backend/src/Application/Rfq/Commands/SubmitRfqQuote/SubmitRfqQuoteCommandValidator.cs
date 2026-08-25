using FluentValidation;

namespace ProcureLive.Application.Rfq.Commands.SubmitRfqQuote;

public sealed class SubmitRfqQuoteCommandValidator : AbstractValidator<SubmitRfqQuoteCommand>
{
    public SubmitRfqQuoteCommandValidator()
    {
        RuleFor(x => x.RfqProcessId).NotEmpty();
        RuleFor(x => x.LineItems).NotEmpty();

        RuleForEach(x => x.LineItems).ChildRules(item =>
        {
            item.RuleFor(i => i.UnitPrice).GreaterThan(0);
            item.RuleFor(i => i.CurrencyId).NotEmpty();
        });
    }
}
