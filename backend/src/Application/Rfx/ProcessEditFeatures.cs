using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfx;

/// <summary>RFI/RFQ iptali, yanıtsız davetin geri alınması ve teklif gelmemiş RFQ kaleminin silinmesi.</summary>
internal static class ProcessEditRules
{
    public const string AlreadyFinal = "Kapanmış, sonuçlanmış veya iptal edilmiş süreç değiştirilemez.";

    public static string WithReason(string text, string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? text : $"{text} Gerekçe: {reason.Trim()}";
}

public sealed record CancelRfiProcessCommand(Guid RfiProcessId, string? Reason) : IRequest, IRfiScopedRequest;

public sealed record CancelRfqProcessCommand(Guid RfqProcessId, string? Reason) : IRequest, IRfqScopedRequest;

public sealed class CancelRfiProcessCommandValidator : AbstractValidator<CancelRfiProcessCommand>
{
    public CancelRfiProcessCommandValidator() => RuleFor(x => x.Reason).MaximumLength(500);
}

public sealed class CancelRfqProcessCommandValidator : AbstractValidator<CancelRfqProcessCommand>
{
    public CancelRfqProcessCommandValidator() => RuleFor(x => x.Reason).MaximumLength(500);
}

public sealed class CancelRfiProcessCommandHandler : IRequestHandler<CancelRfiProcessCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;

    public CancelRfiProcessCommandHandler(IApplicationDbContext context, INotificationRealtimeService realtime, IDateTime dateTime)
    {
        _context = context;
        _realtime = realtime;
        _dateTime = dateTime;
    }

    public async Task Handle(CancelRfiProcessCommand r, CancellationToken cancellationToken)
    {
        RfiProcess p = await _context.RfiProcesses.Include(x => x.Invitations).FirstOrDefaultAsync(x => x.Id == r.RfiProcessId, cancellationToken)
            ?? throw new NotFoundException(nameof(RfiProcess), r.RfiProcessId);
        if (p.Status is not (ProcessStatus.Draft or ProcessStatus.Sent or ProcessStatus.InProgress))
            throw new ConflictException(ProcessEditRules.AlreadyFinal);
        p.Status = ProcessStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        foreach (RfiInvitation inv in p.Invitations.Where(i => i.Status != InvitationStatus.Declined))
        {
            await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, inv.CompanyId, NotificationType.System,
                "RFI İptal Edildi", ProcessEditRules.WithReason($"\"{p.Title}\" RFI süreci iptal edildi.", r.Reason),
                nameof(RfiProcess), p.Id, cancellationToken);
        }
    }
}

public sealed class CancelRfqProcessCommandHandler : IRequestHandler<CancelRfqProcessCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;

    public CancelRfqProcessCommandHandler(IApplicationDbContext context, INotificationRealtimeService realtime, IDateTime dateTime)
    {
        _context = context;
        _realtime = realtime;
        _dateTime = dateTime;
    }

    public async Task Handle(CancelRfqProcessCommand r, CancellationToken cancellationToken)
    {
        RfqProcess p = await _context.RfqProcesses.Include(x => x.Invitations).FirstOrDefaultAsync(x => x.Id == r.RfqProcessId, cancellationToken)
            ?? throw new NotFoundException(nameof(RfqProcess), r.RfqProcessId);
        // Kapanmış ama sonuçlanmamış RFQ da iptal edilebilir (hiçbir teklif seçilmeyecekse).
        if (p.Status is not (ProcessStatus.Draft or ProcessStatus.Sent or ProcessStatus.InProgress or ProcessStatus.Closed))
            throw new ConflictException(ProcessEditRules.AlreadyFinal);
        p.Status = ProcessStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        foreach (RfqInvitation inv in p.Invitations.Where(i => i.Status != InvitationStatus.Declined))
        {
            await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, inv.CompanyId, NotificationType.System,
                "RFQ İptal Edildi", ProcessEditRules.WithReason($"\"{p.Title}\" RFQ süreci iptal edildi.", r.Reason),
                nameof(RfqProcess), p.Id, cancellationToken);
        }
    }
}

// ---------------------------------------------------------------- Davet geri alma

public sealed record RevokeRfiInvitationCommand(Guid RfiProcessId, Guid InvitationId) : IRequest, IRfiScopedRequest;

public sealed record RevokeRfqInvitationCommand(Guid RfqProcessId, Guid InvitationId) : IRequest, IRfqScopedRequest;

public sealed class RevokeRfiInvitationCommandHandler : IRequestHandler<RevokeRfiInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;

    public RevokeRfiInvitationCommandHandler(IApplicationDbContext context, INotificationRealtimeService realtime, IDateTime dateTime)
    {
        _context = context;
        _realtime = realtime;
        _dateTime = dateTime;
    }

    public async Task Handle(RevokeRfiInvitationCommand r, CancellationToken cancellationToken)
    {
        RfiProcess p = await _context.RfiProcesses.FirstOrDefaultAsync(x => x.Id == r.RfiProcessId, cancellationToken)
            ?? throw new NotFoundException(nameof(RfiProcess), r.RfiProcessId);
        if (p.Status is not (ProcessStatus.Draft or ProcessStatus.Sent or ProcessStatus.InProgress))
            throw new ConflictException(ProcessEditRules.AlreadyFinal);
        RfiInvitation inv = await _context.RfiInvitations.FirstOrDefaultAsync(i => i.Id == r.InvitationId && i.RfiProcessId == p.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(RfiInvitation), r.InvitationId);
        if (inv.Status != InvitationStatus.Invited)
            throw new ConflictException("Yalnızca yanıt verilmemiş davet geri alınabilir.");
        _context.RfiInvitations.Remove(inv);
        await _context.SaveChangesAsync(cancellationToken);
        await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, inv.CompanyId, NotificationType.System,
            "Davet Geri Alındı", $"\"{p.Title}\" RFI sürecine davetiniz geri alındı.", null, null, cancellationToken);
    }
}

public sealed class RevokeRfqInvitationCommandHandler : IRequestHandler<RevokeRfqInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;

    public RevokeRfqInvitationCommandHandler(IApplicationDbContext context, INotificationRealtimeService realtime, IDateTime dateTime)
    {
        _context = context;
        _realtime = realtime;
        _dateTime = dateTime;
    }

    public async Task Handle(RevokeRfqInvitationCommand r, CancellationToken cancellationToken)
    {
        RfqProcess p = await _context.RfqProcesses.FirstOrDefaultAsync(x => x.Id == r.RfqProcessId, cancellationToken)
            ?? throw new NotFoundException(nameof(RfqProcess), r.RfqProcessId);
        if (p.Status is not (ProcessStatus.Draft or ProcessStatus.Sent or ProcessStatus.InProgress))
            throw new ConflictException(ProcessEditRules.AlreadyFinal);
        RfqInvitation inv = await _context.RfqInvitations.FirstOrDefaultAsync(i => i.Id == r.InvitationId && i.RfqProcessId == p.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(RfqInvitation), r.InvitationId);
        if (inv.Status != InvitationStatus.Invited)
            throw new ConflictException("Yalnızca yanıt verilmemiş davet geri alınabilir.");
        _context.RfqInvitations.Remove(inv);
        await _context.SaveChangesAsync(cancellationToken);
        await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, inv.CompanyId, NotificationType.System,
            "Davet Geri Alındı", $"\"{p.Title}\" RFQ sürecine davetiniz geri alındı.", null, null, cancellationToken);
    }
}

// ---------------------------------------------------------------- RFQ kalemi silme

public sealed record DeleteRfqLineItemCommand(Guid RfqProcessId, Guid LineItemId) : IRequest, IRfqScopedRequest;

public sealed class DeleteRfqLineItemCommandHandler : IRequestHandler<DeleteRfqLineItemCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteRfqLineItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteRfqLineItemCommand r, CancellationToken cancellationToken)
    {
        RfqProcess p = await _context.RfqProcesses.FirstOrDefaultAsync(x => x.Id == r.RfqProcessId, cancellationToken)
            ?? throw new NotFoundException(nameof(RfqProcess), r.RfqProcessId);
        if (p.Status is not (ProcessStatus.Draft or ProcessStatus.Sent))
            throw new ConflictException("Kalem yalnızca taslak veya henüz yayınlanmamış RFQ'da silinebilir.");
        RfqLineItem line = await _context.RfqLineItems.FirstOrDefaultAsync(l => l.Id == r.LineItemId && l.RfqProcessId == p.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(RfqLineItem), r.LineItemId);
        if (await _context.RfqQuoteLineItems.AnyAsync(q => q.RfqLineItemId == line.Id, cancellationToken))
            throw new ConflictException("Bu kaleme teklif verilmiş; silinemez.");
        _context.RfqLineItems.Remove(line);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
