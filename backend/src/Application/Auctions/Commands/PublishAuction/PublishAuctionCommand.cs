using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Commands.PublishAuction;

public sealed record PublishAuctionCommand(Guid AuctionId) : IRequest, IAuctionScopedRequest;
