using MediatR;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Contracts.Common;

namespace ProcureLive.Application.Auctions.Queries.GetAuctionsList;

public sealed record GetAuctionsListQuery(string? Status, int Page, int PageSize) : IRequest<PagedResult<AuctionDto>>;
