using MediatR;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.CancelAuction;

/// <summary>Geriye dönük uyumlu iptal uç noktası; kurallar, geçmiş ve bildirimler durum makinesinden gelir.</summary>
public sealed class CancelAuctionCommandHandler : IRequestHandler<CancelAuctionCommand>
{
    private readonly AuctionStatusService _statusService;

    public CancelAuctionCommandHandler(AuctionStatusService statusService)
    {
        _statusService = statusService;
    }

    public Task Handle(CancelAuctionCommand request, CancellationToken cancellationToken) =>
        _statusService.ChangeAsync(request.AuctionId, AuctionStatus.Cancelled, request.Reason, null, null, cancellationToken);
}
