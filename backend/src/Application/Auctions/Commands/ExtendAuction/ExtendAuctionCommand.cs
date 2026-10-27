using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Commands.ExtendAuction;

/// <summary>
/// Manual admin intervention: "bitecek ihaleyi butona basarak anında uzatabilme" — pushes EndAt and
/// broadcasts AuctionExtended to every connected client via SignalR immediately.
/// </summary>
public sealed record ExtendAuctionCommand(Guid AuctionId, DateTime NewEndAt, string Reason) : IRequest, IAuctionScopedRequest;
