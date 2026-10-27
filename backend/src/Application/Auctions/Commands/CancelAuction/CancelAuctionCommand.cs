using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Commands.CancelAuction;

public sealed record CancelAuctionCommand(Guid AuctionId, string Reason) : IRequest, IAuctionScopedRequest;
