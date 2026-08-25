using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Commands.RespondToRfqInvitation;

public sealed class RespondToRfqInvitationCommandHandler : IRequestHandler<RespondToRfqInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public RespondToRfqInvitationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task Handle(RespondToRfqInvitationCommand request, CancellationToken cancellationToken)
    {
        Guid? companyId = _currentUserService.CompanyId;

        if (companyId is null)
        {
            throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
        }

        RfqInvitation? invitation = await _context.RfqInvitations
            .FirstOrDefaultAsync(i => i.RfqProcessId == request.RfqProcessId && i.CompanyId == companyId.Value, cancellationToken);

        if (invitation is null)
        {
            throw new NotFoundException(nameof(RfqInvitation), request.RfqProcessId);
        }

        if (invitation.Status != InvitationStatus.Invited)
        {
            throw new ConflictException("Bu davete zaten yanıt verilmiş.");
        }

        DateTime now = _dateTime.UtcNow;
        Guid userId = _currentUserService.UserId ?? Guid.Empty;

        invitation.Status = request.Accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        invitation.RespondedAt = now;

        _context.InvitationResponseLogs.Add(new InvitationResponseLog
        {
            SourceType = InvitationSourceType.Rfq,
            SourceId = request.RfqProcessId,
            CompanyId = companyId.Value,
            UserId = userId,
            ResponseType = request.Accept ? InvitationResponseType.Accepted : InvitationResponseType.Declined,
            Reason = request.Accept ? null : request.DeclineReason,
            RespondedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
