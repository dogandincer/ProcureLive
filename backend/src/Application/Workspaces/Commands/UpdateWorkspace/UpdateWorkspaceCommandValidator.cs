using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Workspaces.Commands.UpdateWorkspace;

public sealed class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .MustAsync(async (cmd, name, ct) =>
                !await context.Workspaces.AnyAsync(w => w.Name == name && w.Id != cmd.Id, ct))
            .WithMessage("Bu isimde bir işyeri zaten mevcut.");

        RuleFor(x => x.Description).MaximumLength(500);
    }
}
