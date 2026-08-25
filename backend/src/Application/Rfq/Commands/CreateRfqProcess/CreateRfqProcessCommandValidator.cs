using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Rfq.Commands.CreateRfqProcess;

public sealed class CreateRfqProcessCommandValidator : AbstractValidator<CreateRfqProcessCommand>
{
    public CreateRfqProcessCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);

        RuleFor(x => x.OwnerCompanyId)
            .MustAsync(async (id, ct) => await context.Companies.AnyAsync(c => c.Id == id, ct))
            .WithMessage("Alıcı Cari bulunamadı.");

        RuleFor(x => x.CurrencyId)
            .MustAsync(async (id, ct) => await context.Currencies.AnyAsync(c => c.Id == id, ct))
            .WithMessage("Para birimi bulunamadı.");
    }
}
