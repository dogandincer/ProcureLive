using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Queries.GetAuctionStatus;

/// <summary>Durum yönetimi paneli: mevcut durum, şu an yapılabilecek geçişler ve tüm durum geçmişi.</summary>
public sealed record GetAuctionStatusQuery(Guid AuctionId) : IRequest<AuctionStatusPanelDto>, IAuctionScopedRequest;

public sealed class GetAuctionStatusQueryHandler : IRequestHandler<GetAuctionStatusQuery, AuctionStatusPanelDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly AuctionStatusService _statusService;
    private readonly ICurrentUserService _currentUser;

    public GetAuctionStatusQueryHandler(
        IApplicationDbContext context, IIdentityService identityService, AuctionStatusService statusService, ICurrentUserService currentUser)
    {
        _context = context;
        _identityService = identityService;
        _statusService = statusService;
        _currentUser = currentUser;
    }

    public async Task<AuctionStatusPanelDto> Handle(GetAuctionStatusQuery request, CancellationToken cancellationToken)
    {
        var auction = await _context.Auctions
            .Where(a => a.Id == request.AuctionId)
            .Select(a => new
            {
                a.Id, a.Status, a.StartAt, a.EndAt, a.ActualStartAt, a.ActualEndAt, a.PausedAt, a.OwnerCompanyId,
                a.ApprovalRequestedAt, a.ApprovalRequestedByUserId, a.ApprovalRejectReason,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), request.AuctionId);

        bool hasAwards = await _context.AuctionAwards.AnyAsync(a => a.AuctionId == auction.Id, cancellationToken);
        bool approvalRequired = auction.Status == AuctionStatus.Draft
            && await _statusService.RequiresPublishApprovalAsync(auction.OwnerCompanyId, cancellationToken);

        // Kazanan seçilmişken bitmiş ihaleyi yeniden açmak/iptal etmek doğrulamada reddedilir; seçenek olarak hiç sunulmaz.
        List<StatusTransitionOptionDto> transitions = AuctionStatusMachine.AllowedFrom(auction.Status)
            .Where(to => !(hasAwards && auction.Status == AuctionStatus.Ended))
            .Where(to => !(approvalRequired && to == AuctionStatus.Scheduled))
            .Where(to => _currentUser.IsInRole(Roles.Admin) || AuctionStatusService.IsBuyerTransition(auction.Status, to))
            .Select(to => new StatusTransitionOptionDto(
                to.ToString(),
                AuctionStatusMachine.RequiresReason(to),
                AuctionStatusMachine.RequiresEndAt(auction.Status, to),
                AuctionStatusMachine.AllowsEndAt(auction.Status, to),
                AuctionStatusMachine.AllowsStartAt(auction.Status, to)))
            .ToList();

        List<AuctionStatusChange> changes = await _context.AuctionStatusChanges
            .Where(c => c.AuctionId == auction.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        var names = new Dictionary<Guid, string>();
        foreach (Guid userId in changes.Where(c => c.ChangedByUserId.HasValue).Select(c => c.ChangedByUserId!.Value).Distinct())
        {
            var user = await _identityService.FindByIdAsync(userId, cancellationToken);
            if (user is not null) names[userId] = $"{user.FirstName} {user.LastName}".Trim();
        }

        List<AuctionStatusChangeDto> history = changes
            .Select(c => new AuctionStatusChangeDto(
                c.Id,
                c.FromStatus.ToString(),
                c.ToStatus.ToString(),
                c.Reason,
                c.ChangedByUserId,
                c.ChangedByUserId is Guid id && names.TryGetValue(id, out string? name) ? name : null,
                c.IsAutomatic,
                c.ChangedAt))
            .ToList();

        string? requestedBy = null;
        if (auction.ApprovalRequestedByUserId is Guid requesterId
            && await _identityService.FindByIdAsync(requesterId, cancellationToken) is { } requester)
        {
            requestedBy = $"{requester.FirstName} {requester.LastName}".Trim();
        }

        return new AuctionStatusPanelDto(
            auction.Id, auction.Status.ToString(), auction.StartAt, auction.EndAt, auction.ActualStartAt, auction.ActualEndAt,
            auction.PausedAt, hasAwards, transitions, history,
            approvalRequired, auction.ApprovalRequestedAt, requestedBy, auction.ApprovalRejectReason);
    }
}
