using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Sectors.Commands.CreateSector;

public sealed class CreateSectorCommandValidator : AbstractValidator<CreateSectorCommand>
{
    public CreateSectorCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .MustAsync(async (name, ct) =>
                !await context.Sectors.AnyAsync(s => s.Name == name, ct))
            .WithMessage("Bu isimde bir sektör zaten mevcut.");

        RuleFor(x => x.Description).MaximumLength(500);
    }
}
