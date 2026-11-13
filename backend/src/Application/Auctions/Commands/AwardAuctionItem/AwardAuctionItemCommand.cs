using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Commands.AwardAuctionItem;

[RequiresPermission(PermissionCodes.AuctionsAward)]
public sealed record AwardAuctionItemCommand(
    Guid AuctionId,
    Guid AuctionItemId,
    Guid WinningBidId,
    string? Notes,
    bool AcknowledgeReserveNotMet = false) : IRequest, IAuctionScopedRequest;
