using MediatR;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.PublishAuction;

/// <summary>
/// Draft -> Scheduled. Durum makinesi yolundan geçer: kalem/davet/tarih kuralları, firma yayın onayı kapısı ve
/// davetlilere "Yeni İhale Daveti" bildirimi elle durum değişikliğiyle aynıdır.
/// </summary>
public sealed class PublishAuctionCommandHandler : IRequestHandler<PublishAuctionCommand>
{
    private readonly AuctionStatusService _statusService;

    public PublishAuctionCommandHandler(AuctionStatusService statusService)
    {
        _statusService = statusService;
    }

    public Task Handle(PublishAuctionCommand request, CancellationToken cancellationToken) =>
        _statusService.ChangeAsync(request.AuctionId, AuctionStatus.Scheduled, null, null, null, cancellationToken);
}
