using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Rfi.Commands.CreateRfiProcess;

public sealed class CreateRfiProcessCommandValidator : AbstractValidator<CreateRfiProcessCommand>
{
    public CreateRfiProcessCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);

        RuleFor(x => x.RfiTemplateId)
            .MustAsync(async (id, ct) => await context.RfiTemplates.AnyAsync(t => t.Id == id, ct))
            .WithMessage("RFI şablonu bulunamadı.");

        RuleFor(x => x.OwnerCompanyId)
            .MustAsync(async (id, ct) => await context.Companies.AnyAsync(c => c.Id == id, ct))
            .WithMessage("Alıcı Cari bulunamadı.");
    }
}
