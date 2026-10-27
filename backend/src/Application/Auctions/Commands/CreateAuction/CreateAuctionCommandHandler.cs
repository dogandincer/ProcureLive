using MediatR;
using ProcureLive.Application.Auctions.Participation;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.CreateAuction;

public sealed class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateAuctionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        Guid? workspaceId = await AuctionWorkspaceRules.ResolveAsync(_context, request.OwnerCompanyId, request.WorkspaceId, cancellationToken);

        var auction = new Auction
        {
            Title = request.Title,
            Description = request.Description,
            AuctionType = request.AuctionType,
            Status = AuctionStatus.Draft,
            OwnerCompanyId = request.OwnerCompanyId,
            WorkspaceId = workspaceId,
            ResponsibleUserId = _currentUserService.UserId,
            CurrencyId = request.CurrencyId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            ReservePrice = request.ReservePrice,
            MinDecrementAmount = request.MinDecrementAmount,
            MinDecrementPercentage = request.MinDecrementPercentage,
            ExtensionRuleMinutes = request.ExtensionRuleMinutes,
            IsListedForPlus = request.IsListedForPlus
        };

        _context.Auctions.Add(auction);
        await _context.SaveChangesAsync(cancellationToken);

        return auction.Id;
    }
}
