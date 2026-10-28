using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Commands.RespondToRfiInvitation;

public sealed class RespondToRfiInvitationCommandHandler : IRequestHandler<RespondToRfiInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _notificationRealtimeService;
    private readonly ISupplierAccessService _accessService;
    private readonly InternalNotificationAudience _audience;

    public RespondToRfiInvitationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        INotificationRealtimeService notificationRealtimeService,
        ISupplierAccessService accessService,
        InternalNotificationAudience audience)
    {
        _audience = audience;
        _accessService = accessService;
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _notificationRealtimeService = notificationRealtimeService;
    }

    public async Task Handle(RespondToRfiInvitationCommand request, CancellationToken cancellationToken)
    {
        Guid? companyId = _currentUserService.CompanyId;

        if (companyId is null)
        {
            throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
        }

        RfiInvitation? invitation = await _context.RfiInvitations
            .FirstOrDefaultAsync(i => i.RfiProcessId == request.RfiProcessId && i.CompanyId == companyId.Value, cancellationToken);

        if (invitation is null)
        {
            throw new NotFoundException(nameof(RfiInvitation), request.RfiProcessId);
        }

        if (invitation.Status != InvitationStatus.Invited)
        {
            throw new ConflictException("Bu davete zaten yanıt verilmiş.");
        }

        // Standart kota dolduysa yeni RFI/RFQ daveti kabul edilemez ("Plus alınız"); reddetmek her zaman serbest.
        if (request.Accept && (await _accessService.GetContextAsync(companyId.Value, cancellationToken)).QuotaReached)
        {
            throw new ConflictException(ProcessParticipationRules.QuotaMessage, ErrorCodes.QuotaExceeded);
        }

        RfiProcess? process = await _context.RfiProcesses.FirstOrDefaultAsync(p => p.Id == request.RfiProcessId, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfiProcess), request.RfiProcessId);
        }

        DateTime now = _dateTime.UtcNow;
        Guid userId = _currentUserService.UserId ?? Guid.Empty;

        invitation.Status = request.Accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        invitation.RespondedAt = now;

        _context.InvitationResponseLogs.Add(new InvitationResponseLog
        {
            SourceType = InvitationSourceType.Rfi,
            SourceId = request.RfiProcessId,
            CompanyId = companyId.Value,
            UserId = userId,
            ResponseType = request.Accept ? InvitationResponseType.Accepted : InvitationResponseType.Declined,
            Reason = request.Accept ? null : request.DeclineReason,
            RespondedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        // Mirrors the Auction/RFQ equivalents - without this, the buyer only finds out a supplier
        // accepted/declined by manually revisiting the RFI detail page.
        string? respondingCompanyName = await _context.Companies
            .Where(c => c.Id == companyId.Value)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        string action = request.Accept ? "kabul etti" : "reddetti";
        string body = $"\"{respondingCompanyName}\" \"{process.Title}\" RFI süreci davetini {action}.";
        if (!request.Accept && !string.IsNullOrWhiteSpace(request.DeclineReason))
        {
            body += $" Gerekçe: {request.DeclineReason}";
        }

        IReadOnlyList<Guid> audience = await _audience.ForScopeAsync(
            process.WorkspaceId, process.OwnerCompanyId, null, process.CreatedBy, cancellationToken);
        await NotificationCreator.CreateForInternalUsersAsync(
            _context, _notificationRealtimeService, _dateTime, audience,
            NotificationType.InvitationResponded,
            "Davet Yanıtlandı",
            body,
            nameof(RfiProcess), process.Id, cancellationToken);
    }
}
