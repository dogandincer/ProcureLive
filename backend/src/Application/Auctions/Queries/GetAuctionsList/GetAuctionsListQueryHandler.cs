using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Contracts.Common;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Queries.GetAuctionsList;

public sealed class GetAuctionsListQueryHandler : IRequestHandler<GetAuctionsListQuery, PagedResult<AuctionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISupplierAccessService _accessService;
    private readonly IDataScopeService _scope;

    public GetAuctionsListQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISupplierAccessService accessService,
        IDataScopeService scope)
    {
        _context = context;
        _currentUserService = currentUserService;
        _accessService = accessService;
        _scope = scope;
    }

    public async Task<PagedResult<AuctionDto>> Handle(GetAuctionsListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Auction> query = _context.Auctions
            .Include(a => a.OwnerCompany)
            .Include(a => a.Currency)
            .Include(a => a.Items)
            .Include(a => a.Invitations)
            .AsQueryable();

        bool isInternalUser = _currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.Buyer);
        SupplierAccessContext? access = null;

        if (!isInternalUser)
        {
            Guid companyId = _currentUserService.CompanyId ?? Guid.Empty;
            access = await _accessService.GetContextAsync(companyId, cancellationToken);

            // Görünürlük kuralı SupplierAccessContext.IsVisible ile birebir aynı (SQL'e çevrilebilir hali).
            List<Guid> directIds = access.InvitedAuctionIds.Union(access.JoinedAuctionIds).ToList();
            List<Guid> ownerIds = access.InvitingOwnerIds.ToList();
            List<Guid> declinedIds = access.DeclinedAuctionIds.ToList();
            bool isPlus = access.IsPlus;
            AuctionStatus[] open = AuctionStatusMachine.OpenStatuses;

            query = query.Where(a =>
                !declinedIds.Contains(a.Id)
                && (directIds.Contains(a.Id)
                    || (open.Contains(a.Status)
                        && (ownerIds.Contains(a.OwnerCompanyId)
                            || (isPlus && a.IsListedForPlus && a.OwnerCompany.ListTendersForPlus)))));
        }

        // Satınalmacı yalnızca yetkili işyerlerinin ihalelerini görür (işyeri atanmamış eski ihalelerde firma yetkisi).
        if (isInternalUser)
        {
            query = query.InScope(await _scope.GetAsync(cancellationToken));
        }

        // İç kullanıcı: başlıktaki firma = ihale sahibi firma; işyeri seçiliyse yalnızca o işyerinin ihaleleri.
        if (isInternalUser && request.WorkspaceId is Guid workspaceId)
        {
            query = query.Where(a => a.WorkspaceId == workspaceId);
        }
        else if (isInternalUser && request.FirmId is Guid firmId)
        {
            query = query.Where(a => a.OwnerCompanyId == firmId);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            AuctionStatus status;
            bool parsed = Enum.TryParse(request.Status, true, out status);

            if (parsed)
            {
                query = query.Where(a => a.Status == status);
            }
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        List<Auction> auctions = await query
            .OrderByDescending(a => a.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // İç kullanıcı listesinde işyeri adı (tek sorgu).
        Dictionary<Guid, string> workspaceNames = new();
        if (isInternalUser)
        {
            List<Guid> workspaceIds = auctions.Where(a => a.WorkspaceId.HasValue).Select(a => a.WorkspaceId!.Value).Distinct().ToList();
            workspaceNames = await _context.Workspaces.Where(w => workspaceIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);
        }

        List<AuctionDto> items = auctions.Select(a => ToDto(a, access, workspaceNames)).ToList();

        return new PagedResult<AuctionDto>(items, totalCount, page, pageSize);
    }

    private static AuctionDto ToDto(Auction a, SupplierAccessContext? access, IReadOnlyDictionary<Guid, string> workspaceNames)
    {
        if (access is null)
        {
            return new AuctionDto(
                a.Id, a.Title, a.Description, a.AuctionType.ToString(), a.Status.ToString(),
                a.OwnerCompanyId, a.OwnerCompany.Name, a.CurrencyId, a.Currency.Code,
                a.StartAt, a.EndAt, a.ActualStartAt, a.ActualEndAt, a.ReservePrice, a.ExtensionRuleMinutes,
                a.Items.Count, a.Invitations.Count, a.CreatedAt,
                AccessLevel: nameof(AuctionAccessLevel.Full), IsListedForPlus: a.IsListedForPlus,
                WorkspaceName: a.WorkspaceId is Guid w && workspaceNames.TryGetValue(w, out string? name) ? name : null);
        }

        var facts = new AuctionAccessFacts(a.Id, a.Status, a.OwnerCompanyId, a.IsListedForPlus, a.OwnerCompany.ListTendersForPlus);
        AuctionAccessLevel level = access.Evaluate(facts);
        bool restricted = level != AuctionAccessLevel.Full;
        string? myStatus = a.Invitations.FirstOrDefault(i => i.CompanyId == access.CompanyId)?.Status.ToString();
        bool requiresApproval = access.CanJoin(facts) && !access.HasActiveRequest(a.Id) && access.NeedsApproval(facts, a.WorkspaceId);

        // Kısıtlı satırda yalnızca vitrin bilgisi kalır: açıklama, rezerv fiyat ve rakip sayısı sunucuda düşürülür.
        return new AuctionDto(
            a.Id, a.Title,
            restricted ? null : a.Description,
            a.AuctionType.ToString(), a.Status.ToString(),
            a.OwnerCompanyId, a.OwnerCompany.Name, a.CurrencyId, a.Currency.Code,
            a.StartAt, a.EndAt, a.ActualStartAt, a.ActualEndAt,
            restricted ? null : a.ReservePrice,
            a.ExtensionRuleMinutes,
            a.Items.Count,
            restricted ? 0 : a.Invitations.Count,
            a.CreatedAt,
            AccessLevel: level.ToString(),
            IsJoined: access.IsJoined(a.Id),
            IsListedForPlus: a.IsListedForPlus,
            MyInvitationStatus: myStatus,
            RequiresApproval: requiresApproval,
            ParticipationStatus: access.RequestStatus(a.Id)?.ToString());
    }
}
