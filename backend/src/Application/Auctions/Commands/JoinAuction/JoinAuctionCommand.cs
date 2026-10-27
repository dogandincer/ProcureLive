using MediatR;

namespace ProcureLive.Application.Auctions.Commands.JoinAuction;

/// <summary>Tedarikçinin görünür ve açık bir ihaleye kendiliğinden katılması (kota kontrollü).</summary>
public sealed record JoinAuctionCommand(Guid AuctionId) : IRequest;
