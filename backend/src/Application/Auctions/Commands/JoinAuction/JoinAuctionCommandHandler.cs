using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.JoinAuction;

public sealed class JoinAuctionCommandHandler : IRequestHandler<JoinAuctionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly ISupplierAccessService _accessService;
    private readonly INotificationRealtimeService _notificationRealtimeService;

    public JoinAuctionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        ISupplierAccessService accessService,
        INotificationRealtimeService notificationRealtimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _accessService = accessService;
        _notificationRealtimeService = notificationRealtimeService;
    }

    public async Task Handle(JoinAuctionCommand request, CancellationToken cancellationToken)
    {
        Guid companyId = _currentUserService.CompanyId
            ?? throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");

        Auction auction = await _context.Auctions
            .Include(a => a.OwnerCompany)
            .FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), request.AuctionId);

        SupplierAccessContext access = await _accessService.GetContextAsync(companyId, cancellationToken);
        var facts = new AuctionAccessFacts(
            auction.Id, auction.Status, auction.OwnerCompanyId, auction.IsListedForPlus, auction.OwnerCompany.ListTendersForPlus);

        if (access.IsJoined(auction.Id))
        {
            throw new ConflictException("Bu ihaleye zaten katıldınız.");
        }

        if (!SupplierAccessContext.IsOpen(auction.Status))
        {
            throw new ConflictException("Yalnızca planlanmış veya canlı ihalelere katılabilirsiniz.", ErrorCodes.AuctionClosed);
        }

        AuctionAccessLevel level = access.Evaluate(facts);
        if (level == AuctionAccessLevel.NoAccess)
        {
            throw new ForbiddenAccessException();
        }

        if (level == AuctionAccessLevel.QuotaExceeded)
        {
            throw new ConflictException(
                $"Standart üyelikte en fazla {SupplierAccessContext.StandardJoinLimit} ihaleye katılabilirsiniz. Daha fazlası için Plus'a geçin.",
                ErrorCodes.QuotaExceeded);
        }

        DateTime now = _dateTime.UtcNow;
        Guid userId = _currentUserService.UserId ?? Guid.Empty;

        AuctionInvitation? invitation = await _context.AuctionInvitations
            .FirstOrDefaultAsync(i => i.AuctionId == auction.Id && i.CompanyId == companyId, cancellationToken);

        if (invitation is null)
        {
            _context.AuctionInvitations.Add(new AuctionInvitation
            {
                AuctionId = auction.Id,
                CompanyId = companyId,
                InvitedByUserId = userId,
                Status = InvitationStatus.Accepted,
                Origin = InvitationOrigin.SelfJoin,
                InvitedAt = now,
                RespondedAt = now,
            });
        }
        else
        {
            invitation.Status = InvitationStatus.Accepted;
            invitation.RespondedAt = now;
        }

        _context.InvitationResponseLogs.Add(new InvitationResponseLog
        {
            SourceType = InvitationSourceType.Auction,
            SourceId = auction.Id,
            CompanyId = companyId,
            UserId = userId,
            ResponseType = InvitationResponseType.Accepted,
            RespondedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        string? companyName = await _context.Companies
            .Where(c => c.Id == companyId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken);

        await NotificationCreator.CreateForInternalUsersAsync(
            _context, _notificationRealtimeService, _dateTime,
            NotificationType.InvitationResponded,
            "İhaleye Katılım",
            $"\"{companyName}\" \"{auction.Title}\" ihalesine katıldı.",
            nameof(Auction), auction.Id, cancellationToken);
    }
}
