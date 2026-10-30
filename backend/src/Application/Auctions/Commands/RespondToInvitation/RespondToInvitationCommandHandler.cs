using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.RespondToInvitation;

public sealed class RespondToInvitationCommandHandler : IRequestHandler<RespondToInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _notificationRealtimeService;
    private readonly ISupplierAccessService _accessService;
    private readonly InternalNotificationAudience _audience;

    public RespondToInvitationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        INotificationRealtimeService notificationRealtimeService,
        ISupplierAccessService accessService,
        InternalNotificationAudience audience)
    {
        _audience = audience;
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _notificationRealtimeService = notificationRealtimeService;
        _accessService = accessService;
    }

    public async Task Handle(RespondToInvitationCommand request, CancellationToken cancellationToken)
    {
        Guid? companyId = _currentUserService.CompanyId;

        if (companyId is null)
        {
            throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
        }

        AuctionInvitation? invitation = await _context.AuctionInvitations
            .FirstOrDefaultAsync(i => i.AuctionId == request.AuctionId && i.CompanyId == companyId.Value, cancellationToken);

        if (invitation is null)
        {
            throw new NotFoundException(nameof(AuctionInvitation), request.AuctionId);
        }

        if (invitation.Status != InvitationStatus.Invited)
        {
            throw new ConflictException("Bu davete zaten yanıt verilmiş.");
        }

        Auction? auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        // Taslak ihaledeki davet tedarikçi için henüz yoktur (yayınlanınca bildirilir).
        if (auction is null || auction.Status == AuctionStatus.Draft)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        if (request.Accept)
        {
            SupplierAccessContext access = await _accessService.GetContextAsync(companyId.Value, cancellationToken);
            if (access.QuotaReached)
            {
                throw new ConflictException(
                    $"Standart üyelikte en fazla {SupplierAccessContext.StandardJoinLimit} ihaleye katılabilirsiniz. Daha fazlası için Plus'a geçin.",
                    ErrorCodes.QuotaExceeded);
            }
        }

        DateTime now = _dateTime.UtcNow;
        Guid userId = _currentUserService.UserId ?? Guid.Empty;

        invitation.Status = request.Accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        invitation.RespondedAt = now;

        _context.InvitationResponseLogs.Add(new InvitationResponseLog
        {
            SourceType = InvitationSourceType.Auction,
            SourceId = request.AuctionId,
            CompanyId = companyId.Value,
            UserId = userId,
            ResponseType = request.Accept ? InvitationResponseType.Accepted : InvitationResponseType.Declined,
            Reason = request.Accept ? null : request.DeclineReason,
            RespondedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        // Mirrors the RFI/RFQ equivalents - without this, the buyer only finds out a supplier
        // accepted/declined by manually revisiting the auction detail page.
        string? respondingCompanyName = await _context.Companies
            .Where(c => c.Id == companyId.Value)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        string action = request.Accept ? "kabul etti" : "reddetti";
        string body = $"\"{respondingCompanyName}\" \"{auction.Title}\" ihalesi davetini {action}.";
        if (!request.Accept && !string.IsNullOrWhiteSpace(request.DeclineReason))
        {
            body += $" Gerekçe: {request.DeclineReason}";
        }

        IReadOnlyList<Guid> audience = await _audience.ForScopeAsync(
            auction.WorkspaceId, auction.OwnerCompanyId, auction.ResponsibleUserId, auction.CreatedBy, cancellationToken);
        await NotificationCreator.CreateForInternalUsersAsync(
            _context, _notificationRealtimeService, _dateTime, audience,
            NotificationType.InvitationResponded,
            "Davet Yanıtlandı",
            body,
            nameof(Auction), auction.Id, cancellationToken);
    }
}
