using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Contracts.Common;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Participation;

/// <summary>
/// İhalenin işyeri ve sorumlu satınalmacısı. Sorumlu adayları: aktif Admin'ler + ihalenin işyerine (işyeri yoksa sahip
/// firmanın herhangi bir işyerine) yetkili aktif satınalmacılar. Katılım talepleri sorumluya düşer.
/// </summary>
internal static class AuctionAssignmentRules
{
    public static async Task<List<ResponsibleCandidateDto>> CandidatesAsync(
        IApplicationDbContext context, IIdentityService identity, Guid ownerFirmId, Guid? workspaceId, CancellationToken ct)
    {
        PagedResult<ApplicationUserInfo> users = await identity.ListUsersAsync(null, null, 1, 500, [Roles.Admin, Roles.Buyer], ct);
        List<ApplicationUserInfo> active = users.Items.Where(u => u.IsActive).ToList();

        List<Guid> buyerIds = active.Where(u => !u.Roles.Contains(Roles.Admin)).Select(u => u.Id).ToList();
        HashSet<Guid> withAccess = (await context.UserWorkspaceAccesses
            .Where(a => buyerIds.Contains(a.UserId)
                && (workspaceId != null ? a.WorkspaceId == workspaceId : a.Workspace.FirmId == ownerFirmId))
            .Select(a => a.UserId)
            .ToListAsync(ct)).ToHashSet();

        return active
            .Where(u => u.Roles.Contains(Roles.Admin) || withAccess.Contains(u.Id))
            .Select(u => new ResponsibleCandidateDto(u.Id, $"{u.FirstName} {u.LastName}".Trim(), u.Email, u.Roles.Contains(Roles.Admin)))
            .OrderBy(u => u.IsAdmin).ThenBy(u => u.FullName)
            .ToList();
    }
}

public sealed record GetResponsibleCandidatesQuery(Guid AuctionId, Guid? WorkspaceId)
    : IRequest<IReadOnlyList<ResponsibleCandidateDto>>, IAuctionScopedRequest, IWorkspaceTargetRequest;

public sealed class GetResponsibleCandidatesQueryHandler : IRequestHandler<GetResponsibleCandidatesQuery, IReadOnlyList<ResponsibleCandidateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identity;

    public GetResponsibleCandidatesQueryHandler(IApplicationDbContext context, IIdentityService identity)
    {
        _context = context;
        _identity = identity;
    }

    public async Task<IReadOnlyList<ResponsibleCandidateDto>> Handle(GetResponsibleCandidatesQuery request, CancellationToken cancellationToken)
    {
        var auction = await _context.Auctions.Where(a => a.Id == request.AuctionId)
            .Select(a => new { a.OwnerCompanyId, a.WorkspaceId }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), request.AuctionId);

        return await AuctionAssignmentRules.CandidatesAsync(
            _context, _identity, auction.OwnerCompanyId, request.WorkspaceId ?? auction.WorkspaceId, cancellationToken);
    }
}

public sealed record UpdateAuctionAssignmentCommand(Guid AuctionId, Guid? WorkspaceId, Guid ResponsibleUserId)
    : IRequest, IAuctionScopedRequest, IWorkspaceTargetRequest;

public sealed class UpdateAuctionAssignmentCommandHandler : IRequestHandler<UpdateAuctionAssignmentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationRealtimeService _realtime;
    private readonly IDateTime _dateTime;

    public UpdateAuctionAssignmentCommandHandler(
        IApplicationDbContext context, IIdentityService identity, ICurrentUserService currentUser,
        INotificationRealtimeService realtime, IDateTime dateTime)
    {
        _context = context;
        _identity = identity;
        _currentUser = currentUser;
        _realtime = realtime;
        _dateTime = dateTime;
    }

    public async Task Handle(UpdateAuctionAssignmentCommand request, CancellationToken cancellationToken)
    {
        Auction auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), request.AuctionId);

        // Onaylı cari değerlendirmesi işyerine bağlı olduğundan işyeri yalnızca katılım başlamadan değiştirilebilir.
        if (request.WorkspaceId != auction.WorkspaceId)
        {
            if (auction.Status is not (AuctionStatus.Draft or AuctionStatus.Scheduled))
                throw new ConflictException("İşyeri yalnızca taslak veya planlanmış ihalede değiştirilebilir.");

            if (request.WorkspaceId is Guid ws
                && !await _context.Workspaces.AnyAsync(w => w.Id == ws && w.FirmId == auction.OwnerCompanyId && w.IsActive, cancellationToken))
                throw new ConflictException("Seçilen işyeri ihale sahibi firmaya ait değil.");

            if (request.WorkspaceId is null && await _context.Workspaces.AnyAsync(w => w.FirmId == auction.OwnerCompanyId, cancellationToken))
                throw new ConflictException("İşyeri seçimi zorunludur.");
        }

        List<ResponsibleCandidateDto> candidates = await AuctionAssignmentRules.CandidatesAsync(
            _context, _identity, auction.OwnerCompanyId, request.WorkspaceId, cancellationToken);
        if (candidates.All(c => c.Id != request.ResponsibleUserId))
            throw new ConflictException("Sorumlu satınalmacı ihalenin işyerine yetkili, aktif bir iç kullanıcı olmalıdır.");

        bool responsibleChanged = auction.ResponsibleUserId != request.ResponsibleUserId;
        auction.WorkspaceId = request.WorkspaceId;
        auction.ResponsibleUserId = request.ResponsibleUserId;
        await _context.SaveChangesAsync(cancellationToken);

        if (responsibleChanged && request.ResponsibleUserId != _currentUser.UserId)
        {
            await NotificationCreator.CreateForInternalUserAsync(
                _context, _realtime, _dateTime, request.ResponsibleUserId, NotificationType.System,
                "İhale Sorumluluğu",
                $"\"{auction.Title}\" ihalesinin sorumlu satınalmacısı olarak atandınız; katılım talepleri size düşecek.",
                nameof(Auction), auction.Id, cancellationToken);
        }
    }
}
