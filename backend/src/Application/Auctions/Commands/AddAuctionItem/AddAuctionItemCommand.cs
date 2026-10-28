using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Commands.AddAuctionItem;

[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record AddAuctionItemCommand(
    Guid AuctionId,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    UnitOfMeasure UnitOfMeasure,
    decimal? TargetPrice,
    int SortOrder) : IRequest<Guid>, IAuctionScopedRequest;
