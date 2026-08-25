using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Commands.InviteCompaniesToRfq;

/// <summary>RFQ setup step 3/4: invite suppliers.</summary>
public sealed class InviteCompaniesToRfqCommandHandler : IRequestHandler<InviteCompaniesToRfqCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _notificationService;

    public InviteCompaniesToRfqCommandHandler(
        IApplicationDbContext context,
        IDateTime dateTime,
        INotificationRealtimeService notificationService)
    {
        _context = context;
        _dateTime = dateTime;
        _notificationService = notificationService;
    }

    public async Task Handle(InviteCompaniesToRfqCommand request, CancellationToken cancellationToken)
    {
        RfqProcess? process = await _context.RfqProcesses.FirstOrDefaultAsync(p => p.Id == request.RfqProcessId, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfqProcess), request.RfqProcessId);
        }

        if (process.Status is not (ProcessStatus.Draft or ProcessStatus.Sent))
        {
            throw new ConflictException("Yalnızca taslak veya gönderilmiş RFQ süreçlerine davet gönderilebilir.");
        }

        List<Guid> alreadyInvited = await _context.RfqInvitations
            .Where(i => i.RfqProcessId == request.RfqProcessId && request.CompanyIds.Contains(i.CompanyId))
            .Select(i => i.CompanyId)
            .ToListAsync(cancellationToken);

        DateTime now = _dateTime.UtcNow;
        List<Guid> newCompanyIds = request.CompanyIds.Except(alreadyInvited).ToList();

        foreach (Guid companyId in newCompanyIds)
        {
            _context.RfqInvitations.Add(new RfqInvitation
            {
                RfqProcessId = request.RfqProcessId,
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
                NotificationType.RfqInvitation,
                "Yeni Hızlı Teklif Talebi (RFQ) Daveti",
                $"\"{process.Title}\" teklif toplama sürecine davet edildiniz.",
                nameof(RfqProcess), process.Id, cancellationToken);
        }
    }
}
