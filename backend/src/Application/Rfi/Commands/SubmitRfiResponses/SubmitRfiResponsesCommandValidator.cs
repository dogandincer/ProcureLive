using FluentValidation;

namespace ProcureLive.Application.Rfi.Commands.SubmitRfiResponses;

public sealed class SubmitRfiResponsesCommandValidator : AbstractValidator<SubmitRfiResponsesCommand>
{
    public SubmitRfiResponsesCommandValidator()
    {
        RuleFor(x => x.RfiProcessId).NotEmpty();
        RuleFor(x => x.Answers).NotEmpty();
    }
}
