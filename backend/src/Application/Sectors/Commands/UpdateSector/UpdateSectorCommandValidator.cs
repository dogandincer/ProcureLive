using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Sectors.Commands.UpdateSector;

public sealed class UpdateSectorCommandValidator : AbstractValidator<UpdateSectorCommand>
{
    public UpdateSectorCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .MustAsync(async (cmd, name, ct) =>
                !await context.Sectors.AnyAsync(s => s.Name == name && s.Id != cmd.Id, ct))
            .WithMessage("Bu isimde bir sektör zaten mevcut.");

        RuleFor(x => x.Description).MaximumLength(500);
    }
}
