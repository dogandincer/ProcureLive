using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Auctions;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Application.Auctions.Queries.GetAuctionById;

public sealed record GetAuctionByIdQuery(Guid Id) : IRequest<AuctionDetailDto>, IRequireResourceAuthorization
{
    public string PolicyName => Policies.AuctionAccess;

    public async Task<object?> LoadResourceAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await context.Auctions
            .Include(a => a.Invitations)
            .FirstOrDefaultAsync(a => a.Id == Id, cancellationToken);
    }
}
