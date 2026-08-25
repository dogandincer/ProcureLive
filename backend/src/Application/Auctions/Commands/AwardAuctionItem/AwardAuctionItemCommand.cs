using MediatR;

namespace ProcureLive.Application.Auctions.Commands.AwardAuctionItem;

public sealed record AwardAuctionItemCommand(
    Guid AuctionId,
    Guid AuctionItemId,
    Guid WinningBidId,
    string? Notes) : IRequest;
