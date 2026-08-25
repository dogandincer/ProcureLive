using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Auctions.Commands.CreateAuction;

public sealed class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.AuctionType).IsInEnum();
        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt).WithMessage("Bitiş tarihi başlangıçtan sonra olmalıdır.");
        RuleFor(x => x.ExtensionRuleMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReservePrice).GreaterThanOrEqualTo(0).When(x => x.ReservePrice.HasValue);

        RuleFor(x => x.OwnerCompanyId)
            .MustAsync(async (id, cancellationToken) => await context.Companies.AnyAsync(c => c.Id == id, cancellationToken))
            .WithMessage("Alıcı Cari bulunamadı.");

        RuleFor(x => x.CurrencyId)
            .MustAsync(async (id, cancellationToken) => await context.Currencies.AnyAsync(c => c.Id == id, cancellationToken))
            .WithMessage("Para birimi bulunamadı.");
    }
}
