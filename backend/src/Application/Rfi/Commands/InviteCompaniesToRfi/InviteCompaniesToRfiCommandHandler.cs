using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Commands.InviteCompaniesToRfi;

public sealed class InviteCompaniesToRfiCommandHandler : IRequestHandler<InviteCompaniesToRfiCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _notificationService;

    public InviteCompaniesToRfiCommandHandler(
        IApplicationDbContext context,
        IDateTime dateTime,
        INotificationRealtimeService notificationService)
    {
        _context = context;
        _dateTime = dateTime;
        _notificationService = notificationService;
    }

    public async Task Handle(InviteCompaniesToRfiCommand request, CancellationToken cancellationToken)
    {
        RfiProcess? process = await _context.RfiProcesses
            .FirstOrDefaultAsync(p => p.Id == request.RfiProcessId, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfiProcess), request.RfiProcessId);
        }

        if (process.Status is not (ProcessStatus.Draft or ProcessStatus.Sent))
        {
            throw new ConflictException("Yalnızca taslak veya gönderilmiş RFI süreçlerine davet gönderilebilir.");
        }

        List<Guid> alreadyInvited = await _context.RfiInvitations
            .Where(i => i.RfiProcessId == request.RfiProcessId && request.CompanyIds.Contains(i.CompanyId))
            .Select(i => i.CompanyId)
            .ToListAsync(cancellationToken);

        DateTime now = _dateTime.UtcNow;
        List<Guid> newCompanyIds = request.CompanyIds.Except(alreadyInvited).ToList();

        foreach (Guid companyId in newCompanyIds)
        {
            _context.RfiInvitations.Add(new RfiInvitation
            {
                RfiProcessId = request.RfiProcessId,
                CompanyId = companyId,
                Status = InvitationStatus.Invited,
                SentAt = now
            });
        }

        if (newCompanyIds.Count > 0 && process.Status == ProcessStatus.Draft)
        {
            process.Status = ProcessStatus.Sent;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (Guid companyId in newCompanyIds)
        {
            await NotificationCreator.CreateForCompanyAsync(
                _context, _notificationService, _dateTime, companyId,
                NotificationType.RfiInvitation,
                "Yeni Bilgi Talebi (RFI) Daveti",
                $"\"{process.Title}\" bilgi talebi sürecine davet edildiniz.",
                nameof(RfiProcess), process.Id, cancellationToken);
        }
    }
}
