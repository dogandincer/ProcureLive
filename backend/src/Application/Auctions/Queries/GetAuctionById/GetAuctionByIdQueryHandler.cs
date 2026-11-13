using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Participation;
using ProcureLive.Application.Auctions.Reserve;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Queries.GetAuctionById;

public sealed class GetAuctionByIdQueryHandler : IRequestHandler<GetAuctionByIdQuery, AuctionDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISupplierAccessService _accessService;
    private readonly IIdentityService _identityService;

    public GetAuctionByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISupplierAccessService accessService,
        IIdentityService identityService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _accessService = accessService;
        _identityService = identityService;
    }

    public async Task<AuctionDetailDto> Handle(GetAuctionByIdQuery request, CancellationToken cancellationToken)
    {
        Auction? auction = await _context.Auctions
            .Include(a => a.OwnerCompany)
            .Include(a => a.Currency)
            .Include(a => a.Items)
            .Include(a => a.Invitations).ThenInclude(i => i.Company)
            .Include(a => a.Attachments)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.Id);
        }

        bool isInternalUser = _currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.Buyer);
        Guid? viewerCompanyId = _currentUserService.CompanyId;
        AuctionAccessLevel level = AuctionAccessLevel.Full;
        bool isJoined = false;
        bool canJoin = false;
        bool requiresApproval = false;
        ParticipationRequestDto? myRequest = null;

        // Taslak ihale yalnızca iç kullanıcıya görünür; tedarikçiye başlığı dahil hiçbir bilgi dönmez.
        if (!isInternalUser && auction.Status == AuctionStatus.Draft)
        {
            throw new NotFoundException(nameof(Auction), request.Id);
        }

        if (!isInternalUser)
        {
            if (viewerCompanyId is not Guid companyId)
            {
                return Restricted(auction, AuctionAccessLevel.NoAccess);
            }

            SupplierAccessContext access = await _accessService.GetContextAsync(companyId, cancellationToken);
            var facts = new AuctionAccessFacts(
                auction.Id, auction.Status, auction.OwnerCompanyId, auction.IsListedForPlus, auction.OwnerCompany.ListTendersForPlus);

            level = access.Evaluate(facts);
            if (level != AuctionAccessLevel.Full)
            {
                // Teklifler hiç sorgulanmaz; kalem/fiyat/davetli/ek verisi istemciye ulaşmaz.
                return Restricted(auction, level);
            }

            isJoined = access.IsJoined(auction.Id);

            // Onaylı cari / davetli doğrudan katılır; diğerleri katılım talebi + ön teklif gönderir.
            bool eligible = access.CanJoin(facts) && !access.HasActiveRequest(auction.Id);
            bool needsApproval = access.NeedsApproval(facts, auction.WorkspaceId);
            canJoin = eligible && !needsApproval;
            requiresApproval = eligible && needsApproval;

            if (access.RequestStatus(auction.Id) is not null)
            {
                myRequest = (await ParticipationRules.ToDtosAsync(
                    _context.ParticipationRequests.Where(r => r.AuctionId == auction.Id && r.CompanyId == companyId),
                    cancellationToken)).FirstOrDefault();
            }
        }

        // Sorumlu satınalmacı ve işyeri yalnızca iç kullanıcıya döner.
        string? workspaceName = null;
        string? responsibleName = null;
        if (isInternalUser)
        {
            if (auction.WorkspaceId is Guid wsId)
            {
                workspaceName = await _context.Workspaces.Where(w => w.Id == wsId).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken);
            }

            if (auction.ResponsibleUserId is Guid responsibleId
                && await _identityService.FindByIdAsync(responsibleId, cancellationToken) is { } responsible)
            {
                responsibleName = $"{responsible.FirstName} {responsible.LastName}".Trim();
            }
        }

        // Tüm kalemlerin teklifleri tek sorguda (kalem başına sorgu/N+1 yerine), bellekte gruplanır.
        List<Guid> itemIds = auction.Items.Select(i => i.Id).ToList();
        Dictionary<Guid, List<Bid>> bidsByItem = (await _context.Bids
                .Where(b => itemIds.Contains(b.AuctionItemId))
                .ToListAsync(cancellationToken))
            .GroupBy(b => b.AuctionItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Kapalı zarfta tedarikçi rakiplerin tutarını ve sırasını görmez; hedef fiyat iç bilgidir.
        bool sealedType = auction.AuctionType is AuctionType.SealedBid or AuctionType.ReverseSealed;
        bool higherIsBetter = BidRanking.HigherIsBetter(auction.AuctionType);

        List<AuctionItemDto> itemDtos = auction.Items
            .OrderBy(i => i.SortOrder)
            .Select(item =>
            {
                List<Bid> bids = bidsByItem.TryGetValue(item.Id, out List<Bid>? b) ? b : new List<Bid>();
                Bid? best = BidRanking.Best(bids, auction.AuctionType);
                if (isInternalUser)
                {
                    return new AuctionItemDto(
                        item.Id, item.ProductId, item.Description, item.Quantity, item.UnitOfMeasure.ToString(),
                        item.TargetPrice, item.RequestedDeliveryDate, item.Specification, item.SortOrder,
                        best?.Amount.Amount, bids.Count);
                }

                // Firma başına en iyi birim fiyat → kendi en iyisi ve sırası.
                List<(Guid CompanyId, decimal Amount)> bestPerCompany = bids
                    .GroupBy(x => x.CompanyId)
                    .Select(g => (g.Key, higherIsBetter ? g.Max(x => x.Amount.Amount) : g.Min(x => x.Amount.Amount)))
                    .ToList();
                decimal? mine = bestPerCompany.Where(x => x.CompanyId == viewerCompanyId).Select(x => (decimal?)x.Amount).FirstOrDefault();
                int? rank = null;
                if (mine is decimal m && !sealedType)
                {
                    rank = 1 + bestPerCompany.Count(x => x.CompanyId != viewerCompanyId && (higherIsBetter ? x.Amount > m : x.Amount < m));
                }

                return new AuctionItemDto(
                    item.Id, item.ProductId, item.Description, item.Quantity, item.UnitOfMeasure.ToString(),
                    null, item.RequestedDeliveryDate, item.Specification, item.SortOrder,
                    sealedType ? null : best?.Amount.Amount, bids.Count,
                    mine, rank, sealedType ? null : bestPerCompany.Count);
            })
            .ToList();

        // Tedarikçi rakip firmaların kimliğini görmez: yalnızca kendi davet kaydı döner.
        List<AuctionInvitationDto> invitationDtos = auction.Invitations
            .Where(i => isInternalUser || i.CompanyId == viewerCompanyId)
            .Select(i => new AuctionInvitationDto(
                i.Id, i.CompanyId, i.Company.Name, i.Status.ToString(), i.InvitedAt, i.RespondedAt, i.Origin.ToString()))
            .ToList();

        List<AuctionAttachmentDto> attachmentDtos = auction.Attachments
            .Select(a => new AuctionAttachmentDto(a.Id, a.FileName, a.Url, a.ContentType, a.SizeBytes))
            .ToList();

        ReserveEvaluation? reserve = isInternalUser
            ? await ReservePriceEvaluator.EvaluateAsync(
                _context, auction, auction.Items.ToList(), bidsByItem.Values.SelectMany(x => x).ToList(), null, cancellationToken)
            : null;

        return new AuctionDetailDto(
            auction.Id,
            auction.Title,
            auction.ReferenceCode,
            auction.Description,
            auction.AuctionType.ToString(),
            auction.Status.ToString(),
            auction.OwnerCompanyId,
            auction.OwnerCompany.Name,
            auction.CurrencyId,
            auction.Currency.Code,
            auction.StartAt,
            auction.EndAt,
            auction.ActualStartAt,
            auction.ActualEndAt,
            isInternalUser ? auction.ReservePrice : null,
            auction.MinDecrementAmount,
            auction.MinDecrementPercentage,
            auction.ExtensionRuleMinutes,
            auction.ExtensionDurationMinutes,
            auction.MaxExtensionMinutes,
            auction.PaymentTerm?.ToString(),
            auction.DeliveryTerm?.ToString(),
            auction.AllowAlternativeProducts,
            auction.AllowPartialBids,
            itemDtos,
            invitationDtos,
            attachmentDtos,
            AccessLevel: level.ToString(),
            IsJoined: isJoined,
            CanJoin: canJoin,
            IsListedForPlus: auction.IsListedForPlus,
            ItemCount: auction.Items.Count,
            RequiresApproval: requiresApproval,
            MyParticipationRequest: myRequest,
            WorkspaceId: isInternalUser ? auction.WorkspaceId : null,
            WorkspaceName: workspaceName,
            ResponsibleUserId: isInternalUser ? auction.ResponsibleUserId : null,
            ResponsibleUserName: responsibleName,
            ReserveStatus: reserve?.Status.ToString(),
            ReserveTotal: reserve?.Total,
            ReserveCoveredItems: reserve?.CoveredItems ?? 0);
    }

    /// <summary>
    /// Kısıtlı görünüm. QuotaExceeded: vitrin bilgisi (başlık, tip, sahip, tarihler, kalem sayısı) kalır.
    /// NoAccess: içerik hiç dönmez — yalnızca kimlik ve durum. Kalemler, fiyatlar, davetliler, ekler,
    /// şartlar ve açıklama her iki durumda da sunucuda düşürülür.
    /// </summary>
    private static AuctionDetailDto Restricted(Auction a, AuctionAccessLevel level)
    {
        bool teaser = level == AuctionAccessLevel.QuotaExceeded;

        return new AuctionDetailDto(
            a.Id,
            teaser ? a.Title : string.Empty,
            teaser ? a.ReferenceCode : null,
            null,
            a.AuctionType.ToString(),
            a.Status.ToString(),
            teaser ? a.OwnerCompanyId : Guid.Empty,
            teaser ? a.OwnerCompany.Name : string.Empty,
            teaser ? a.CurrencyId : Guid.Empty,
            teaser ? a.Currency.Code : string.Empty,
            teaser ? a.StartAt : default,
            teaser ? a.EndAt : default,
            null,
            null,
            null,
            null,
            null,
            0,
            null,
            null,
            null,
            null,
            false,
            false,
            Array.Empty<AuctionItemDto>(),
            Array.Empty<AuctionInvitationDto>(),
            Array.Empty<AuctionAttachmentDto>(),
            AccessLevel: level.ToString(),
            IsJoined: false,
            CanJoin: false,
            IsListedForPlus: false,
            ItemCount: teaser ? a.Items.Count : 0);
    }
}
