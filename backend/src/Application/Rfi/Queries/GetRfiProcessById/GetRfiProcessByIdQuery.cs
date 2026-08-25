using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiProcessById;

public sealed record GetRfiProcessByIdQuery(Guid Id) : IRequest<RfiProcessDetailDto>, IRequireResourceAuthorization
{
    public string PolicyName => Policies.RfiProcessAccess;

    public async Task<object?> LoadResourceAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await context.RfiProcesses
            .Include(p => p.Invitations)
            .FirstOrDefaultAsync(p => p.Id == Id, cancellationToken);
    }
}
