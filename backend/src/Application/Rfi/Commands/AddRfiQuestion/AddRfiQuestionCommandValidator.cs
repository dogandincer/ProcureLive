using FluentValidation;

namespace ProcureLive.Application.Rfi.Commands.AddRfiQuestion;

public sealed class AddRfiQuestionCommandValidator : AbstractValidator<AddRfiQuestionCommand>
{
    public AddRfiQuestionCommandValidator()
    {
        RuleFor(x => x.RfiTemplateId).NotEmpty();
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.QuestionType).IsInEnum();
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0);
    }
}
