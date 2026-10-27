using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.InviteCompanies;

public sealed class InviteCompaniesCommandHandler : IRequestHandler<InviteCompaniesCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _notificationRealtimeService;

    public InviteCompaniesCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        INotificationRealtimeService notificationRealtimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _notificationRealtimeService = notificationRealtimeService;
    }

    public async Task Handle(InviteCompaniesCommand request, CancellationToken cancellationToken)
    {
        Auction? auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        if (auction.Status is not (AuctionStatus.Draft or AuctionStatus.Scheduled))
        {
            throw new ConflictException("Yalnızca taslak veya planlanmış ihalelere davet gönderilebilir.");
        }

        List<AuctionInvitation> existing = await _context.AuctionInvitations
            .Where(i => i.AuctionId == request.AuctionId && request.CompanyIds.Contains(i.CompanyId))
            .ToListAsync(cancellationToken);
        List<Guid> alreadyInvited = existing.Select(i => i.CompanyId).ToList();

        // Vitrinden kendiliğinden katılmış cariyi doğrudan eklemek onu gerçek davetliye çevirir.
        foreach (AuctionInvitation selfJoined in existing.Where(i => i.Origin == InvitationOrigin.SelfJoin))
        {
            selfJoined.Origin = InvitationOrigin.Direct;
        }

        DateTime now = _dateTime.UtcNow;
        Guid invitedByUserId = _currentUserService.UserId ?? Guid.Empty;

        List<Guid> newlyInvitedCompanyIds = request.CompanyIds.Except(alreadyInvited).ToList();

        foreach (Guid companyId in newlyInvitedCompanyIds)
        {
            _context.AuctionInvitations.Add(new AuctionInvitation
            {
                AuctionId = request.AuctionId,
                CompanyId = companyId,
                InvitedByUserId = invitedByUserId,
                Status = InvitationStatus.Invited,
                InvitedAt = now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Mirrors InviteCompaniesToRfiCommandHandler / InviteCompaniesToRfqCommandHandler - without this,
        // a supplier invited to an auction (the app's core feature) never finds out unless they happen
        // to be browsing the list themselves.
        foreach (Guid companyId in newlyInvitedCompanyIds)
        {
            await NotificationCreator.CreateForCompanyAsync(
                _context, _notificationRealtimeService, _dateTime, companyId,
                NotificationType.AuctionInvitation,
                "Yeni İhale Daveti",
                $"\"{auction.Title}\" ihalesine davet edildiniz.",
                nameof(Auction), auction.Id, cancellationToken);
        }
    }
}
