using MediatR;
using ProcureLive.Application.Workspaces;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.CreateAuction;

public sealed class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDataScopeService _scope;

    public CreateAuctionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IDataScopeService scope)
    {
        _context = context;
        _currentUserService = currentUserService;
        _scope = scope;
    }

    public async Task<Guid> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        Guid? workspaceId = await FirmWorkspaceRules.ResolveAsync(
            _context, request.OwnerCompanyId, request.WorkspaceId, await _scope.GetAsync(cancellationToken), cancellationToken);

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
