using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Auctions.Commands.PlaceBid;

public sealed class PlaceBidCommandValidator : AbstractValidator<PlaceBidCommand>
{
    public PlaceBidCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.AuctionItemId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);

        RuleFor(x => x.CurrencyId)
            .MustAsync(async (id, cancellationToken) => await context.Currencies.AnyAsync(c => c.Id == id, cancellationToken))
            .WithMessage("Para birimi bulunamadı.");
    }
}
