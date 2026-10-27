using FluentValidation;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Workspaces.Commands.CreateWorkspace;

public sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator(IApplicationDbContext context)
    {
        WorkspaceRules.AddFirmAndNameRules(this, context, _ => null, x => x.FirmId, x => x.Name);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
