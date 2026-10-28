using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Queries.GetAuctionFeed;

/// <summary>
/// Canlı akışın son teklifleri (sayfa açılınca boş görünmesin). İç kullanıcı firma adı + tutarı görür; tedarikçi yalnızca
/// kendi teklifini adıyla, rakipleri anonim, kapalı zarfta tutarsız görür. Tedarikçi erişimi detay sayfasıyla aynı kurala bağlı.
/// </summary>
public sealed record GetAuctionFeedQuery(Guid AuctionId, int Take = 25) : IRequest<IReadOnlyList<AuctionFeedEntryDto>>, IAuctionScopedRequest;

public sealed class GetAuctionFeedQueryHandler : IRequestHandler<GetAuctionFeedQuery, IReadOnlyList<AuctionFeedEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISupplierAccessService _accessService;

    public GetAuctionFeedQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISupplierAccessService accessService)
    {
        _context = context;
        _currentUser = currentUser;
        _accessService = accessService;
    }

    public async Task<IReadOnlyList<AuctionFeedEntryDto>> Handle(GetAuctionFeedQuery request, CancellationToken cancellationToken)
    {
        var auction = await _context.Auctions
            .Where(a => a.Id == request.AuctionId)
            .Select(a => new { a.Id, a.Status, a.AuctionType, a.OwnerCompanyId, a.IsListedForPlus, OwnerLists = a.OwnerCompany.ListTendersForPlus, Currency = a.Currency.Code })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Auction), request.AuctionId);

        bool isInternal = _currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Buyer);
        Guid? viewer = _currentUser.CompanyId;
        if (!isInternal)
        {
            if (viewer is not Guid companyId)
                throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
            SupplierAccessContext access = await _accessService.GetContextAsync(companyId, cancellationToken);
            var facts = new AuctionAccessFacts(auction.Id, auction.Status, auction.OwnerCompanyId, auction.IsListedForPlus, auction.OwnerLists);
            if (access.Evaluate(facts) != AuctionAccessLevel.Full)
                return [];
        }

        bool sealedType = auction.AuctionType is AuctionType.SealedBid or AuctionType.ReverseSealed;
        var rows = await _context.Bids
            .Where(b => b.AuctionItem.AuctionId == request.AuctionId)
            .OrderByDescending(b => b.BidTime)
            .Take(Math.Clamp(request.Take, 1, 100))
            .Select(b => new { b.Id, b.AuctionItemId, Item = b.AuctionItem.Description, b.CompanyId, Company = b.Company.Name, Amount = b.Amount.Amount, b.BidTime })
            .ToListAsync(cancellationToken);

        return rows.Select(r =>
        {
            bool mine = !isInternal && r.CompanyId == viewer;
            return new AuctionFeedEntryDto(
                r.Id, r.AuctionItemId, r.Item,
                isInternal || mine ? r.Company : null,
                isInternal || mine || !sealedType ? r.Amount : null,
                auction.Currency, mine, r.BidTime);
        }).ToList();
    }
}
