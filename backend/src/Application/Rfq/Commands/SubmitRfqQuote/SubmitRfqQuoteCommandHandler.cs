using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;
using ProcureLive.Domain.ValueObjects;

namespace ProcureLive.Application.Rfq.Commands.SubmitRfqQuote;

public sealed class SubmitRfqQuoteCommandHandler : IRequestHandler<SubmitRfqQuoteCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public SubmitRfqQuoteCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<Guid> Handle(SubmitRfqQuoteCommand request, CancellationToken cancellationToken)
    {
        Guid? companyId = _currentUserService.CompanyId;

        if (companyId is null)
        {
            throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
        }

        RfqInvitation? invitation = await _context.RfqInvitations
            .FirstOrDefaultAsync(i => i.RfqProcessId == request.RfqProcessId && i.CompanyId == companyId.Value, cancellationToken);

        bool canQuote = invitation is not null
            && (invitation.Status == InvitationStatus.Accepted || invitation.Status == InvitationStatus.Participating);

        if (!canQuote)
        {
            throw new ForbiddenAccessException("Teklif verebilmek için davetin kabul edilmiş olması gerekir.");
        }

        List<Guid> validLineItemIds = await _context.RfqLineItems
            .Where(l => l.RfqProcessId == request.RfqProcessId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        foreach (var lineItem in request.LineItems)
        {
            if (!validLineItemIds.Contains(lineItem.RfqLineItemId))
            {
                throw new ConflictException("Teklif verilen kalem bu RFQ sürecine ait değil.");
            }
        }

        RfqQuote? quote = await _context.RfqQuotes
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.RfqProcessId == request.RfqProcessId && q.CompanyId == companyId.Value, cancellationToken);

        DateTime now = _dateTime.UtcNow;
        Guid userId = _currentUserService.UserId ?? Guid.Empty;

        if (quote is null)
        {
            quote = new RfqQuote
            {
                RfqProcessId = request.RfqProcessId,
                CompanyId = companyId.Value,
                SubmittedByUserId = userId,
                Status = RfqQuoteStatus.Submitted,
                SubmittedAt = now,
                ValidUntil = request.ValidUntil,
                Notes = request.Notes
            };

            _context.RfqQuotes.Add(quote);
        }
        else
        {
            quote.Status = RfqQuoteStatus.Submitted;
            quote.SubmittedAt = now;
            quote.ValidUntil = request.ValidUntil;
            quote.Notes = request.Notes;

            foreach (RfqQuoteLineItem existingLine in quote.LineItems.ToList())
            {
                _context.RfqQuoteLineItems.Remove(existingLine);
            }

            quote.LineItems.Clear();
        }

        foreach (var lineItem in request.LineItems)
        {
            quote.LineItems.Add(new RfqQuoteLineItem
            {
                RfqLineItemId = lineItem.RfqLineItemId,
                UnitPrice = new Money(lineItem.UnitPrice, lineItem.CurrencyId),
                LeadTimeDays = lineItem.LeadTimeDays,
                Notes = lineItem.Notes
            });
        }

        if (invitation!.Status == InvitationStatus.Accepted)
        {
            invitation.Status = InvitationStatus.Participating;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return quote.Id;
    }
}
