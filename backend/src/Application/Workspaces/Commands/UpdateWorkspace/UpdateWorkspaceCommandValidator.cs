using FluentValidation;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Workspaces.Commands.UpdateWorkspace;

public sealed class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator(IApplicationDbContext context)
    {
        WorkspaceRules.AddFirmAndNameRules(this, context, x => x.Id, x => x.FirmId, x => x.Name);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
