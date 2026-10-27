using MediatR;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Queries.GetAuctionById;

/// <summary>
/// Erişim kontrolü handler içinde (SupplierAccessContext): yetkisi olmayan veya kotası dolan tedarikçiye
/// 403 yerine hassas alanları sunucuda düşürülmüş "kısıtlı" DTO döner (istemci bulanıklaştırır).
/// </summary>
public sealed record GetAuctionByIdQuery(Guid Id) : IRequest<AuctionDetailDto>, IAuctionScopedRequest
{
    Guid IAuctionScopedRequest.AuctionId => Id;
}
