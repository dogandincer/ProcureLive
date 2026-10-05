using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Workspaces.Commands.CreateWorkspace;

public sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .MustAsync(async (name, ct) =>
                !await context.Workspaces.AnyAsync(w => w.Name == name, ct))
            .WithMessage("Bu isimde bir işyeri zaten mevcut.");

        RuleFor(x => x.Description).MaximumLength(500);
    }
}
