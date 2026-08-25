using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Rfq;

namespace ProcureLive.Application.Rfq.Queries.GetRfqProcessById;

public sealed record GetRfqProcessByIdQuery(Guid Id) : IRequest<RfqProcessDetailDto>, IRequireResourceAuthorization
{
    public string PolicyName => Policies.RfqProcessAccess;

    public async Task<object?> LoadResourceAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await context.RfqProcesses
            .Include(p => p.Invitations)
            .FirstOrDefaultAsync(p => p.Id == Id, cancellationToken);
    }
}
