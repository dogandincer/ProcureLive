using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Participation;

/// <summary>
/// Katılım talebi akışı (davetsiz / firmanın onaylı carisi olmayan tedarikçi):
///   1) Tedarikçi talep + kalem bazında ön teklif gönderir (Pending)            → sorumlu satınalmacıya ve tedarikçiye bildirim
///   2) Satınalmacı cariyi onaylar: cari aktif olur, ihalenin işyerinde görünür   → CompanyApproved, iki tarafa bildirim
///   3) Satınalmacı ön teklifi inceleyip kabul eder: tedarikçi ihaleye dahil olur → Accepted (davet Origin=Request), iki tarafa bildirim
///   Herhangi bir adımda red (Rejected, gerekçeli)                               → iki tarafa bildirim
/// </summary>
public static class ParticipationRules
{
    public static async Task<List<ParticipationRequestDto>> ToDtosAsync(
        IQueryable<ParticipationRequest> query, CancellationToken ct)
    {
        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.AuctionId,
                AuctionTitle = r.Auction.Title,
                AuctionWorkspaceId = r.Auction.WorkspaceId,
                CurrencyCode = r.Auction.Currency.Code,
                r.CompanyId,
                CompanyName = r.Company.Name,
                CompanyCode = r.Company.Code,
                r.Company.TaxNumber,
                CompanyStatus = r.Company.Status,
                CompanyWorkspaceId = r.Company.WorkspaceId,
                Visible = r.Company.VisibleWorkspaces.Select(v => v.WorkspaceId).ToList(),
                r.Status,
                r.Note,
                r.CreatedAt,
                r.CompanyApprovedAt,
                r.DecidedAt,
                r.RejectReason,
                r.PreOfferAppliedAt,
                Lines = r.Lines.Select(l => new
                {
                    l.AuctionItemId,
                    l.AuctionItem.Description,
                    l.AuctionItem.Quantity,
                    l.AuctionItem.UnitOfMeasure,
                    l.AuctionItem.SortOrder,
                    l.UnitPrice,
                    l.Note,
                    l.BidId,
                }).ToList(),
            })
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            bool approved = r.AuctionWorkspaceId is Guid w
                ? r.CompanyStatus != CompanyStatus.Passive && (r.CompanyWorkspaceId == w || r.Visible.Contains(w))
                : r.Status is ParticipationRequestStatus.CompanyApproved or ParticipationRequestStatus.Accepted;

            List<ParticipationRequestLineDto> lines = r.Lines
                .OrderBy(l => l.SortOrder)
                .Select(l => new ParticipationRequestLineDto(
                    l.AuctionItemId, l.Description, l.Quantity, l.UnitOfMeasure.ToString(), l.UnitPrice,
                    Math.Round(l.UnitPrice * l.Quantity, 2), l.Note,
                    r.PreOfferAppliedAt is null ? null : l.BidId is not null))
                .ToList();

            return new ParticipationRequestDto(
                r.Id, r.AuctionId, r.AuctionTitle, r.CompanyId, r.CompanyName, r.CompanyCode, r.TaxNumber,
                r.CompanyStatus.ToString(), approved, r.Status.ToString(), r.Note, r.CurrencyCode,
                lines.Sum(l => l.LineTotal), r.CreatedAt, r.CompanyApprovedAt, r.DecidedAt, r.RejectReason, lines,
                r.PreOfferAppliedAt);
        }).ToList();
    }

    /// <summary>Satınalmacı adımları için talebi ihale ve cari ile yükler (ihale kimliği eşleşmeli).</summary>
    internal static async Task<ParticipationRequest> LoadAsync(IApplicationDbContext context, Guid auctionId, Guid requestId, CancellationToken ct) =>
        await context.ParticipationRequests
            .Include(r => r.Auction).ThenInclude(a => a.OwnerCompany)
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.AuctionId == auctionId, ct)
        ?? throw new NotFoundException(nameof(ParticipationRequest), requestId);

    /// <summary>Her adımda iki tarafa bildirim: tedarikçi carisine ve sorumlu satınalmacıya (yoksa tüm iç kullanıcılara).</summary>
    internal static async Task NotifyBothAsync(
        IApplicationDbContext context, INotificationRealtimeService realtime, IDateTime dateTime, ParticipationRequest request,
        string supplierTitle, string supplierBody, string buyerTitle, string buyerBody, CancellationToken ct)
    {
        await NotificationCreator.CreateForCompanyAsync(
            context, realtime, dateTime, request.CompanyId, NotificationType.ParticipationRequest,
            supplierTitle, supplierBody, nameof(Auction), request.AuctionId, ct);

        await NotificationCreator.CreateForInternalUserAsync(
            context, realtime, dateTime, request.Auction.ResponsibleUserId, NotificationType.ParticipationRequest,
            buyerTitle, buyerBody, nameof(Auction), request.AuctionId, ct);
    }
}

// ---------------------------------------------------------------- 1) Talep + ön teklif (tedarikçi)

public sealed record CreateParticipationRequestCommand(Guid AuctionId, string? Note, IReadOnlyList<ParticipationLineInput> Lines)
    : IRequest<Guid>;

public sealed class CreateParticipationRequestCommandValidator : AbstractValidator<CreateParticipationRequestCommand>
{
    public CreateParticipationRequestCommandValidator()
    {
        RuleFor(x => x.Note).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("En az bir kalem için ön teklif girin.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.UnitPrice).GreaterThan(0).WithMessage("Ön teklif birim fiyatı sıfırdan büyük olmalıdır.");
            line.RuleFor(l => l.Note).MaximumLength(500);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.AuctionItemId).Distinct().Count() == lines.Count)
            .WithMessage("Aynı kalem birden fazla kez girilemez.");
    }
}

public sealed class CreateParticipationRequestCommandHandler : IRequestHandler<CreateParticipationRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISupplierAccessService _accessService;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;

    public CreateParticipationRequestCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, ISupplierAccessService accessService,
        INotificationRealtimeService realtime, IDateTime dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _accessService = accessService;
        _realtime = realtime;
        _dateTime = dateTime;
    }

    public async Task<Guid> Handle(CreateParticipationRequestCommand request, CancellationToken cancellationToken)
    {
        Guid companyId = _currentUser.CompanyId
            ?? throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");

        Auction auction = await _context.Auctions
            .Include(a => a.OwnerCompany)
            .Include(a => a.Items)
            .FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), request.AuctionId);

        SupplierAccessContext access = await _accessService.GetContextAsync(companyId, cancellationToken);
        var facts = new AuctionAccessFacts(
            auction.Id, auction.Status, auction.OwnerCompanyId, auction.IsListedForPlus, auction.OwnerCompany.ListTendersForPlus);

        if (access.IsJoined(auction.Id))
            throw new ConflictException("Bu ihaleye zaten katıldınız.");

        if (!SupplierAccessContext.IsOpen(auction.Status))
            throw new ConflictException("Yalnızca planlanmış veya canlı ihalelere katılım talebi gönderebilirsiniz.", ErrorCodes.AuctionClosed);

        AuctionAccessLevel level = access.Evaluate(facts);
        if (level == AuctionAccessLevel.NoAccess)
            throw new ForbiddenAccessException();

        if (level == AuctionAccessLevel.QuotaExceeded)
            throw new ConflictException(
                $"Standart üyelikte en fazla {SupplierAccessContext.StandardJoinLimit} ihaleye katılabilirsiniz. Daha fazlası için Plus'a geçin.",
                ErrorCodes.QuotaExceeded);

        if (access.HasActiveRequest(auction.Id))
            throw new ConflictException("Bu ihale için değerlendirilmekte olan bir katılım talebiniz var.");

        if (!access.NeedsApproval(facts, auction.WorkspaceId))
            throw new ConflictException("Bu ihaleye onay gerekmeden doğrudan katılabilirsiniz.");

        HashSet<Guid> itemIds = auction.Items.Select(i => i.Id).ToHashSet();
        if (request.Lines.Any(l => !itemIds.Contains(l.AuctionItemId)))
            throw new ConflictException("Ön teklifte bu ihaleye ait olmayan bir kalem var.");

        if (!auction.AllowPartialBids && request.Lines.Count != itemIds.Count)
            throw new ConflictException("Bu ihalede kısmi teklif kapalı; tüm kalemler için ön teklif vermelisiniz.");

        var participation = new ParticipationRequest
        {
            AuctionId = auction.Id,
            Auction = auction,
            CompanyId = companyId,
            RequestedByUserId = _currentUser.UserId ?? Guid.Empty,
            Status = ParticipationRequestStatus.Pending,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };

        foreach (ParticipationLineInput line in request.Lines)
        {
            participation.Lines.Add(new ParticipationRequestLine
            {
                AuctionItemId = line.AuctionItemId,
                UnitPrice = line.UnitPrice,
                Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(),
            });
        }

        _context.ParticipationRequests.Add(participation);
        await _context.SaveChangesAsync(cancellationToken);

        string companyName = await _context.Companies.Where(c => c.Id == companyId).Select(c => c.Name).FirstAsync(cancellationToken);

        await ParticipationRules.NotifyBothAsync(
            _context, _realtime, _dateTime, participation,
            "Katılım Talebiniz İletildi",
            $"\"{auction.Title}\" ihalesi için katılım talebiniz ve ön teklifiniz sorumlu satınalmacıya iletildi.",
            "Yeni Katılım Talebi",
            $"\"{companyName}\" \"{auction.Title}\" ihalesine katılmak için cari onayı ve ön teklif gönderdi.",
            cancellationToken);

        return participation.Id;
    }
}

// ---------------------------------------------------------------- Satınalmacı: talepleri listele

public sealed record GetParticipationRequestsQuery(Guid AuctionId) : IRequest<IReadOnlyList<ParticipationRequestDto>>, IAuctionScopedRequest;

public sealed class GetParticipationRequestsQueryHandler : IRequestHandler<GetParticipationRequestsQuery, IReadOnlyList<ParticipationRequestDto>>
{
    private readonly IApplicationDbContext _context;

    public GetParticipationRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ParticipationRequestDto>> Handle(GetParticipationRequestsQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.Auctions.AnyAsync(a => a.Id == request.AuctionId, cancellationToken))
            throw new NotFoundException(nameof(Auction), request.AuctionId);

        return await ParticipationRules.ToDtosAsync(
            _context.ParticipationRequests.Where(r => r.AuctionId == request.AuctionId), cancellationToken);
    }
}

// ---------------------------------------------------------------- 2) Cari onayı

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record ApproveParticipationCompanyCommand(Guid AuctionId, Guid RequestId) : IRequest, IAuctionScopedRequest;

public sealed class ApproveParticipationCompanyCommandHandler : IRequestHandler<ApproveParticipationCompanyCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;

    public ApproveParticipationCompanyCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, INotificationRealtimeService realtime, IDateTime dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _realtime = realtime;
        _dateTime = dateTime;
    }

    public async Task Handle(ApproveParticipationCompanyCommand request, CancellationToken cancellationToken)
    {
        ParticipationRequest participation = await ParticipationRules.LoadAsync(_context, request.AuctionId, request.RequestId, cancellationToken);

        if (participation.Status != ParticipationRequestStatus.Pending)
            throw new ConflictException("Cari onayı yalnızca bekleyen talepler için verilebilir.");

        Company company = participation.Company;
        if (company.Status == CompanyStatus.Passive)
            throw new ConflictException("Pasif durumdaki cari onaylanamaz; önce Cari kartından aktifleştirin.");

        company.Status = CompanyStatus.Active;

        // Cari ihalenin işyerinde görünür olur: birincil işyeri yoksa o olur, varsa ek işyeri bağı eklenir.
        if (participation.Auction.WorkspaceId is Guid workspaceId)
        {
            if (company.WorkspaceId is null)
            {
                company.WorkspaceId = workspaceId;
            }
            else if (company.WorkspaceId != workspaceId
                && !await _context.CompanyWorkspaces.AnyAsync(x => x.CompanyId == company.Id && x.WorkspaceId == workspaceId, cancellationToken))
            {
                _context.CompanyWorkspaces.Add(new CompanyWorkspace { CompanyId = company.Id, WorkspaceId = workspaceId });
            }
        }

        DateTime now = _dateTime.UtcNow;
        participation.Status = ParticipationRequestStatus.CompanyApproved;
        participation.CompanyApprovedAt = now;
        participation.CompanyApprovedByUserId = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        await ParticipationRules.NotifyBothAsync(
            _context, _realtime, _dateTime, participation,
            "Cari Kaydınız Onaylandı",
            $"\"{participation.Auction.OwnerCompany.Name}\" cari kaydınızı onayladı. \"{participation.Auction.Title}\" için ön teklifiniz değerlendiriliyor.",
            "Cari Onaylandı",
            $"\"{company.Name}\" cari olarak onaylandı; \"{participation.Auction.Title}\" için ön teklifi kabul bekliyor.",
            cancellationToken);
    }
}

// ---------------------------------------------------------------- 3) Ön teklifi kabul et → ihaleye dahil et

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record AcceptParticipationRequestCommand(Guid AuctionId, Guid RequestId) : IRequest, IAuctionScopedRequest;

public sealed class AcceptParticipationRequestCommandHandler : IRequestHandler<AcceptParticipationRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISupplierAccessService _accessService;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;
    private readonly PreOfferBidService _preOffers;

    public AcceptParticipationRequestCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, ISupplierAccessService accessService,
        INotificationRealtimeService realtime, IDateTime dateTime, PreOfferBidService preOffers)
    {
        _context = context;
        _currentUser = currentUser;
        _accessService = accessService;
        _realtime = realtime;
        _dateTime = dateTime;
        _preOffers = preOffers;
    }

    public async Task Handle(AcceptParticipationRequestCommand request, CancellationToken cancellationToken)
    {
        ParticipationRequest participation = await ParticipationRules.LoadAsync(_context, request.AuctionId, request.RequestId, cancellationToken);

        if (participation.Status == ParticipationRequestStatus.Pending)
            throw new ConflictException("Ön teklifi kabul etmeden önce cari onayı verilmelidir.");
        if (participation.Status != ParticipationRequestStatus.CompanyApproved)
            throw new ConflictException("Bu talep zaten sonuçlandırılmış.");

        Auction auction = participation.Auction;
        if (!SupplierAccessContext.IsOpen(auction.Status))
            throw new ConflictException("İhale artık açık değil; tedarikçi dahil edilemez.", ErrorCodes.AuctionClosed);

        // Kota, kabul anında tedarikçinin güncel katılımlarına göre yeniden kontrol edilir.
        SupplierAccessContext access = await _accessService.GetContextAsync(participation.CompanyId, cancellationToken);
        if (access.QuotaReached && !access.IsJoined(auction.Id))
            throw new ConflictException(
                "Tedarikçinin Standart üyelik katılım kotası dolu; Plus'a geçmeden ihaleye dahil edilemez.", ErrorCodes.QuotaExceeded);

        DateTime now = _dateTime.UtcNow;
        Guid userId = _currentUser.UserId ?? Guid.Empty;

        AuctionInvitation? invitation = await _context.AuctionInvitations
            .FirstOrDefaultAsync(i => i.AuctionId == auction.Id && i.CompanyId == participation.CompanyId, cancellationToken);

        if (invitation is null)
        {
            _context.AuctionInvitations.Add(new AuctionInvitation
            {
                AuctionId = auction.Id,
                CompanyId = participation.CompanyId,
                InvitedByUserId = userId,
                Status = InvitationStatus.Accepted,
                Origin = InvitationOrigin.Request,
                InvitedAt = now,
                RespondedAt = now,
            });
        }
        else
        {
            invitation.Status = InvitationStatus.Accepted;
            invitation.Origin = InvitationOrigin.Request;
            invitation.RespondedAt = now;
        }

        _context.InvitationResponseLogs.Add(new InvitationResponseLog
        {
            SourceType = InvitationSourceType.Auction,
            SourceId = auction.Id,
            CompanyId = participation.CompanyId,
            UserId = participation.RequestedByUserId,
            ResponseType = InvitationResponseType.Accepted,
            RespondedAt = now
        });

        participation.Status = ParticipationRequestStatus.Accepted;
        participation.DecidedAt = now;
        participation.DecidedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        await ParticipationRules.NotifyBothAsync(
            _context, _realtime, _dateTime, participation,
            "İhaleye Dahil Edildiniz",
            $"Ön teklifiniz kabul edildi; \"{auction.Title}\" ihalesine katılımcı olarak dahil edildiniz.",
            "Katılımcı Eklendi",
            $"\"{participation.Company.Name}\" ön teklifi kabul edilerek \"{auction.Title}\" ihalesine dahil edildi.",
            cancellationToken);

        // İhale zaten canlıysa ön teklif hemen ilk teklif olur; değilse canlıya geçişte işlenir.
        if (auction.Status == AuctionStatus.Live)
        {
            await _preOffers.ApplyAsync(auction.Id, cancellationToken);
        }
    }
}

// ---------------------------------------------------------------- Red

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record RejectParticipationRequestCommand(Guid AuctionId, Guid RequestId, string? Reason) : IRequest, IAuctionScopedRequest;

public sealed class RejectParticipationRequestCommandValidator : AbstractValidator<RejectParticipationRequestCommand>
{
    public RejectParticipationRequestCommandValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class RejectParticipationRequestCommandHandler : IRequestHandler<RejectParticipationRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;

    public RejectParticipationRequestCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, INotificationRealtimeService realtime, IDateTime dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _realtime = realtime;
        _dateTime = dateTime;
    }

    public async Task Handle(RejectParticipationRequestCommand request, CancellationToken cancellationToken)
    {
        ParticipationRequest participation = await ParticipationRules.LoadAsync(_context, request.AuctionId, request.RequestId, cancellationToken);

        if (participation.Status is ParticipationRequestStatus.Accepted or ParticipationRequestStatus.Rejected)
            throw new ConflictException("Bu talep zaten sonuçlandırılmış.");

        string? reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        participation.Status = ParticipationRequestStatus.Rejected;
        participation.RejectReason = reason;
        participation.DecidedAt = _dateTime.UtcNow;
        participation.DecidedByUserId = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        string suffix = reason is null ? string.Empty : $" Gerekçe: {reason}";
        await ParticipationRules.NotifyBothAsync(
            _context, _realtime, _dateTime, participation,
            "Katılım Talebiniz Reddedildi",
            $"\"{participation.Auction.Title}\" ihalesi için katılım talebiniz reddedildi.{suffix}",
            "Katılım Talebi Reddedildi",
            $"\"{participation.Company.Name}\" firmasının \"{participation.Auction.Title}\" katılım talebi reddedildi.{suffix}",
            cancellationToken);
    }
}
