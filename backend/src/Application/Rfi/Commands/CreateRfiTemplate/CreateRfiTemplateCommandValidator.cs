using FluentValidation;

namespace ProcureLive.Application.Rfi.Commands.CreateRfiTemplate;

public sealed class CreateRfiTemplateCommandValidator : AbstractValidator<CreateRfiTemplateCommand>
{
    public CreateRfiTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
    }
}
