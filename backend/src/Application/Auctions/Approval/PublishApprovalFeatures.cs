using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Approval;

/// <summary>
/// Yayın onayı: firması "yayın onayı gerekir" olan satınalmacı taslağı onaya gönderir; Admin onaylarsa ihale planlanır
/// (normal yayın kuralları geçerlidir), reddederse gerekçe taslakta görünür ve talep eden bilgilendirilir.
/// </summary>
[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record RequestPublishApprovalCommand(Guid AuctionId, DateTime? NewStartAt = null, DateTime? NewEndAt = null)
    : IRequest, IAuctionScopedRequest;

public sealed record ApprovePublishCommand(Guid AuctionId) : IRequest, IAuctionScopedRequest;

public sealed record RejectPublishCommand(Guid AuctionId, string Reason) : IRequest, IAuctionScopedRequest;

public sealed class RequestPublishApprovalCommandValidator : AbstractValidator<RequestPublishApprovalCommand>
{
    public RequestPublishApprovalCommandValidator() =>
        RuleFor(x => x.NewEndAt).GreaterThan(x => x.NewStartAt!.Value)
            .When(x => x.NewStartAt.HasValue && x.NewEndAt.HasValue)
            .WithMessage("Bitiş tarihi başlangıçtan sonra olmalıdır.");
}

public sealed class RejectPublishCommandValidator : AbstractValidator<RejectPublishCommand>
{
    public RejectPublishCommandValidator() =>
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Ret gerekçesi zorunludur.").MaximumLength(500);
}

public sealed class RequestPublishApprovalCommandHandler : IRequestHandler<RequestPublishApprovalCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _realtime;
    private readonly InternalNotificationAudience _audience;

    public RequestPublishApprovalCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, IDateTime dateTime,
        INotificationRealtimeService realtime, InternalNotificationAudience audience)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _realtime = realtime;
        _audience = audience;
    }

    public async Task Handle(RequestPublishApprovalCommand r, CancellationToken cancellationToken)
    {
        Auction auction = await _context.Auctions.Include(a => a.Items).Include(a => a.Invitations)
            .FirstOrDefaultAsync(a => a.Id == r.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), r.AuctionId);
        if (auction.Status != AuctionStatus.Draft)
            throw new ConflictException("Yalnızca taslak ihale onaya gönderilebilir.");
        if (auction.ApprovalRequestedAt is not null)
            throw new ConflictException("Bu ihale zaten onay bekliyor.", ErrorCodes.RequestPending);
        AuctionStatusService.EnsurePublishable(auction);
        // Onay penceresinde seçilen plan taslağa yazılır; Admin onayladığında bu tarihlerle planlanır.
        if (r.NewStartAt is DateTime start) auction.StartAt = start.ToUniversalTime();
        if (r.NewEndAt is DateTime end) auction.EndAt = end.ToUniversalTime();
        if (auction.StartAt <= _dateTime.UtcNow)
            throw new ConflictException("Başlangıç tarihi gelecekte olmalıdır.");
        if (auction.EndAt <= auction.StartAt)
            throw new ConflictException("Bitiş tarihi başlangıçtan sonra olmalıdır.");

        auction.ApprovalRequestedAt = _dateTime.UtcNow;
        auction.ApprovalRequestedByUserId = _currentUser.UserId;
        auction.ApprovalRejectReason = null;
        await _context.SaveChangesAsync(cancellationToken);

        await NotificationCreator.CreateForInternalUsersAsync(_context, _realtime, _dateTime, await _audience.AdminsAsync(cancellationToken),
            NotificationType.PublishApproval, "Yayın Onayı Bekleniyor",
            $"\"{auction.Title}\" ihalesi yayın onayınızı bekliyor.", nameof(Auction), auction.Id, cancellationToken);
    }
}

public sealed class ApprovePublishCommandHandler : IRequestHandler<ApprovePublishCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly AuctionStatusService _statusService;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _realtime;

    public ApprovePublishCommandHandler(
        IApplicationDbContext context, AuctionStatusService statusService, IDateTime dateTime, INotificationRealtimeService realtime)
    {
        _context = context;
        _statusService = statusService;
        _dateTime = dateTime;
        _realtime = realtime;
    }

    public async Task Handle(ApprovePublishCommand r, CancellationToken cancellationToken)
    {
        Auction auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == r.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), r.AuctionId);
        if (auction.ApprovalRequestedAt is null)
            throw new ConflictException("Bu ihale için bekleyen bir yayın onayı talebi yok.");

        // Onaylayan Admin olduğundan kapı devreye girmez; kalem/davet/tarih kuralları normal yayınla aynıdır.
        // Yayın, talep alanlarını temizler; talep eden önceden alınır.
        Guid? requester = auction.ApprovalRequestedByUserId;
        await _statusService.ChangeAsync(auction.Id, AuctionStatus.Scheduled, null, null, null, cancellationToken);

        if (requester is Guid userId)
        {
            await NotificationCreator.CreateForInternalUserAsync(_context, _realtime, _dateTime, userId, NotificationType.PublishApproval,
                "Yayın Onaylandı", $"\"{auction.Title}\" ihalesinin yayını onaylandı ve ihale planlandı.", nameof(Auction), auction.Id, cancellationToken);
        }
    }
}

public sealed class RejectPublishCommandHandler : IRequestHandler<RejectPublishCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _realtime;

    public RejectPublishCommandHandler(IApplicationDbContext context, IDateTime dateTime, INotificationRealtimeService realtime)
    {
        _context = context;
        _dateTime = dateTime;
        _realtime = realtime;
    }

    public async Task Handle(RejectPublishCommand r, CancellationToken cancellationToken)
    {
        Auction auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == r.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), r.AuctionId);
        if (auction.ApprovalRequestedAt is null)
            throw new ConflictException("Bu ihale için bekleyen bir yayın onayı talebi yok.");

        Guid? requester = auction.ApprovalRequestedByUserId;
        auction.ApprovalRequestedAt = null;
        auction.ApprovalRequestedByUserId = null;
        auction.ApprovalRejectReason = r.Reason.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        if (requester is Guid userId)
        {
            await NotificationCreator.CreateForInternalUserAsync(_context, _realtime, _dateTime, userId, NotificationType.PublishApproval,
                "Yayın Reddedildi", $"\"{auction.Title}\" ihalesinin yayın talebi reddedildi. Gerekçe: {auction.ApprovalRejectReason}",
                nameof(Auction), auction.Id, cancellationToken);
        }
    }
}

public sealed record GetPendingPublishApprovalsQuery : IRequest<IReadOnlyList<Contracts.Auctions.PendingPublishApprovalDto>>;

public sealed class GetPendingPublishApprovalsQueryHandler
    : IRequestHandler<GetPendingPublishApprovalsQuery, IReadOnlyList<Contracts.Auctions.PendingPublishApprovalDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identity;

    public GetPendingPublishApprovalsQueryHandler(IApplicationDbContext context, IIdentityService identity)
    {
        _context = context;
        _identity = identity;
    }

    public async Task<IReadOnlyList<Contracts.Auctions.PendingPublishApprovalDto>> Handle(
        GetPendingPublishApprovalsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _context.Auctions.AsNoTracking()
            .Where(a => a.Status == AuctionStatus.Draft && a.ApprovalRequestedAt != null)
            .OrderBy(a => a.ApprovalRequestedAt)
            .Select(a => new { a.Id, a.Title, Owner = a.OwnerCompany.Name, a.StartAt, RequestedAt = a.ApprovalRequestedAt!.Value, a.ApprovalRequestedByUserId })
            .ToListAsync(cancellationToken);

        var result = new List<Contracts.Auctions.PendingPublishApprovalDto>();
        foreach (var r in rows)
        {
            string? name = r.ApprovalRequestedByUserId is Guid uid && await _identity.FindByIdAsync(uid, cancellationToken) is { } u
                ? $"{u.FirstName} {u.LastName}".Trim()
                : null;
            result.Add(new(r.Id, r.Title, r.Owner, r.StartAt, r.RequestedAt, name));
        }
        return result;
    }
}
