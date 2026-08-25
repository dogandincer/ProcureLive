using FluentValidation;

namespace ProcureLive.Application.Auctions.Commands.InviteCompanies;

public sealed class InviteCompaniesCommandValidator : AbstractValidator<InviteCompaniesCommand>
{
    public InviteCompaniesCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.CompanyIds).NotEmpty();
    }
}
