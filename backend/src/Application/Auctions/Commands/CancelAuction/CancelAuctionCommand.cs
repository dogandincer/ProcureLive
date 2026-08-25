using MediatR;

namespace ProcureLive.Application.Auctions.Commands.CancelAuction;

public sealed record CancelAuctionCommand(Guid AuctionId, string Reason) : IRequest;
