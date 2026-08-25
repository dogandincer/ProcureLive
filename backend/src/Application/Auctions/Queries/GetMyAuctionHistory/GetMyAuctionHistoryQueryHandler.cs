using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Contracts.Common;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Queries.GetMyAuctionHistory;

public sealed class GetMyAuctionHistoryQueryHandler : IRequestHandler<GetMyAuctionHistoryQuery, PagedResult<AuctionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyAuctionHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<AuctionDto>> Handle(GetMyAuctionHistoryQuery request, CancellationToken cancellationToken)
    {
        Guid? companyId = _currentUserService.CompanyId;

        if (companyId is null)
        {
            throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
        }

        AuctionStatus[] completedStatuses = { AuctionStatus.Ended, AuctionStatus.Awarded, AuctionStatus.Cancelled };

        IQueryable<Auction> query = _context.Auctions
            .Include(a => a.OwnerCompany)
            .Include(a => a.Currency)
            .Include(a => a.Items)
            .Include(a => a.Invitations)
            .Where(a => completedStatuses.Contains(a.Status)
                && a.Invitations.Any(i => i.CompanyId == companyId.Value
                    && (i.Status == InvitationStatus.Participating
                        || i.Status == InvitationStatus.Won
                        || i.Status == InvitationStatus.Lost)));

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        List<Auction> auctions = await query
            .OrderByDescending(a => a.EndAt)
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
