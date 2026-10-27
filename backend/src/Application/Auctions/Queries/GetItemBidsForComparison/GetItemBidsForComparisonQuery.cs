using MediatR;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Queries.GetItemBidsForComparison;

/// <summary>
/// Admin evaluation screen: all bids on an item, normalized to the base currency so bids placed
/// in different currencies can be compared side by side before picking a winner.
/// </summary>
public sealed record GetItemBidsForComparisonQuery(Guid AuctionId, Guid AuctionItemId)
    : IRequest<IReadOnlyCollection<BidComparisonDto>>, IAuctionScopedRequest;
