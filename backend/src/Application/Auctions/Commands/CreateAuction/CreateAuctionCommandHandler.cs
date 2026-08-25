using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.CreateAuction;

public sealed class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateAuctionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = new Auction
        {
            Title = request.Title,
            Description = request.Description,
            AuctionType = request.AuctionType,
            Status = AuctionStatus.Draft,
            OwnerCompanyId = request.OwnerCompanyId,
            CurrencyId = request.CurrencyId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            ReservePrice = request.ReservePrice,
            MinDecrementAmount = request.MinDecrementAmount,
            MinDecrementPercentage = request.MinDecrementPercentage,
            ExtensionRuleMinutes = request.ExtensionRuleMinutes
        };

        _context.Auctions.Add(auction);
        await _context.SaveChangesAsync(cancellationToken);

        return auction.Id;
    }
}
