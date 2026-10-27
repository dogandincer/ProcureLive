using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

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
    int ExtensionRuleMinutes,
    bool IsListedForPlus = false) : IRequest<Guid>, IOwnerFirmScopedRequest;
