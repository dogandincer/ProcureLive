using MediatR;
using ProcureLive.Contracts.Auctions;

namespace ProcureLive.Application.Auctions.Commands.PlaceBid;

public sealed record PlaceBidCommand(Guid AuctionId, Guid AuctionItemId, decimal Amount, Guid CurrencyId) : IRequest<BidDto>;
