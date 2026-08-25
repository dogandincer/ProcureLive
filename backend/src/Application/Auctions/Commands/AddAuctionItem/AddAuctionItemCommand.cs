using MediatR;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.AddAuctionItem;

public sealed record AddAuctionItemCommand(
    Guid AuctionId,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    UnitOfMeasure UnitOfMeasure,
    decimal? TargetPrice,
    int SortOrder) : IRequest<Guid>;
