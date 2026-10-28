using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Commands.CreateAuction;

[RequiresPermission(PermissionCodes.AuctionsManage)]
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
    bool IsListedForPlus = false,
    Guid? WorkspaceId = null) : IRequest<Guid>, IOwnerFirmScopedRequest, IWorkspaceTargetRequest;
