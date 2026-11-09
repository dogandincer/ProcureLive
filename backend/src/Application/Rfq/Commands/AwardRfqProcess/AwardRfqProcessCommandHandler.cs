using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Commands.AwardRfqProcess;

public sealed class AwardRfqProcessCommandHandler : IRequestHandler<AwardRfqProcessCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _notificationService;

    public AwardRfqProcessCommandHandler(
        IApplicationDbContext context,
        IDateTime dateTime,
        INotificationRealtimeService notificationService)
    {
        _context = context;
        _dateTime = dateTime;
        _notificationService = notificationService;
    }

    public async Task Handle(AwardRfqProcessCommand request, CancellationToken cancellationToken)
    {
        RfqProcess? process = await _context.RfqProcesses
            .Include(p => p.Invitations)
            .Include(p => p.Quotes)
            .FirstOrDefaultAsync(p => p.Id == request.RfqProcessId, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfqProcess), request.RfqProcessId);
        }

        if (process.Status != ProcessStatus.Closed)
        {
            throw new ConflictException("Yalnızca kapatılmış RFQ süreçlerinde kazanan seçilebilir.");
        }

        RfqQuote? winningQuote = process.Quotes.FirstOrDefault(
            q => q.Id == request.WinningQuoteId && q.Status == RfqQuoteStatus.Submitted);

        if (winningQuote is null)
        {
            throw new NotFoundException(nameof(RfqQuote), request.WinningQuoteId);
        }

        DateTime now = _dateTime.UtcNow;

        process.Status = ProcessStatus.Awarded;
        process.WinningQuoteId = winningQuote.Id;
        process.AwardedAt = now;
        process.AwardNotes = request.Notes;

        foreach (RfqInvitation invitation in process.Invitations.Where(i => i.Status != InvitationStatus.Declined))
        {
            invitation.Status = invitation.CompanyId == winningQuote.CompanyId
                ? InvitationStatus.Won
                : InvitationStatus.Lost;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await NotificationCreator.CreateForCompanyAsync(
            _context, _notificationService, _dateTime, winningQuote.CompanyId,
            NotificationType.AwardNotice,
            "Teklifiniz Kazandı",
            $"\"{process.Title}\" RFQ sürecinde teklifiniz kazandı.",
            nameof(RfqProcess), process.Id, cancellationToken);

        List<Guid> losingCompanyIds = process.Quotes
            .Where(q => q.Status == RfqQuoteStatus.Submitted && q.CompanyId != winningQuote.CompanyId)
            .Select(q => q.CompanyId)
            .Distinct()
            .ToList();

        foreach (Guid companyId in losingCompanyIds)
        {
            await NotificationCreator.CreateForCompanyAsync(
                _context, _notificationService, _dateTime, companyId,
                NotificationType.AwardNotice,
                "RFQ Sonuçlandı",
                // Teşekkür metni bildirimin içinde: bildirim e-postası kaybedene ayrı bir teşekkür e-postasının yerini tutar.
                $"\"{process.Title}\" RFQ sürecinde başka bir tedarikçinin teklifi seçildi. Katılımınız ve teklifiniz için teşekkür ederiz.",
                nameof(RfqProcess), process.Id, cancellationToken);
        }
    }
}
