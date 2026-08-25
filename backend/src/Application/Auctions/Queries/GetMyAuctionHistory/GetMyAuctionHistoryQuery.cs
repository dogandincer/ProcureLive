using MediatR;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Contracts.Common;

namespace ProcureLive.Application.Auctions.Queries.GetMyAuctionHistory;

/// <summary>"Kişiler, önceden teklif verdikleri tamamlanmış ihaleleri kendi panellerinden görebilmeli."</summary>
public sealed record GetMyAuctionHistoryQuery(int Page, int PageSize) : IRequest<PagedResult<AuctionDto>>;
