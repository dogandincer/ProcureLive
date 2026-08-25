using MediatR;

namespace ProcureLive.Application.Auctions.Commands.PublishAuction;

public sealed record PublishAuctionCommand(Guid AuctionId) : IRequest;
