using MediatR;
using Microsoft.EntityFrameworkCore;
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

    public GetAuctionsListQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        if (!isInternalUser)
        {
            Guid companyId = _currentUserService.CompanyId ?? Guid.Empty;
            query = query.Where(a => a.Invitations.Any(i =>
                i.CompanyId == companyId
                && (i.Status == InvitationStatus.Accepted
                    || i.Status == InvitationStatus.Participating
                    || i.Status == InvitationStatus.Won
                    || i.Status == InvitationStatus.Lost)));
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

        List<AuctionDto> items = auctions.Select(a => new AuctionDto(
            a.Id,
            a.Title,
            a.Description,
            a.AuctionType.ToString(),
            a.Status.ToString(),
            a.OwnerCompanyId,
            a.OwnerCompany.Name,
            a.CurrencyId,
            a.Currency.Code,
            a.StartAt,
            a.EndAt,
            a.ActualStartAt,
            a.ActualEndAt,
            a.ReservePrice,
            a.ExtensionRuleMinutes,
            a.Items.Count,
            a.Invitations.Count,
            a.CreatedAt)).ToList();

        return new PagedResult<AuctionDto>(items, totalCount, page, pageSize);
    }
}
