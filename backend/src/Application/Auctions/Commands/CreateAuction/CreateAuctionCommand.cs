using MediatR;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.CreateAuction;

public sealed record CreateAuctionCommand(
    string Title,
    string? Description,
    AuctionType AuctionType,
    Guid OwnerCompanyId,
    Guid CurrencyId,
    DateTime StartAt,
    DateTime EndAt,
    decimal? ReservePrice,
    decimal? MinDecrementAmount,
    decimal? MinDecrementPercentage,
    int ExtensionRuleMinutes) : IRequest<Guid>;
