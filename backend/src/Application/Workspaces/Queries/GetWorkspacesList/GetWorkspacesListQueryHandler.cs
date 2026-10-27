using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Workspaces;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Workspaces.Queries.GetWorkspacesList;

public sealed class GetWorkspacesListQueryHandler : IRequestHandler<GetWorkspacesListQuery, List<WorkspaceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDataScopeService _scope;

    public GetWorkspacesListQueryHandler(IApplicationDbContext context, IDataScopeService scope)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<List<WorkspaceDto>> Handle(GetWorkspacesListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ProcureLive.Domain.Entities.Workspaces.Workspace> query = _context.Workspaces.AsQueryable();

        if (request.ActiveOnly == true)
        {
            query = query.Where(w => w.IsActive);
        }

        // Satınalmacı yalnızca kendisine atanan işyerlerini görür.
        DataScope scope = await _scope.GetAsync(cancellationToken);
        if (!scope.IsUnrestricted)
        {
            List<Guid> allowed = scope.WorkspaceIds.ToList();
            query = query.Where(w => allowed.Contains(w.Id));
        }

        if (request.FirmId is Guid firmId)
        {
            query = query.Where(w => w.FirmId == firmId);
        }

        return await query
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .Select(w => new WorkspaceDto(
                w.Id,
                w.Name,
                w.Description,
                w.IsActive,
                _context.Companies.Count(c => c.WorkspaceId == w.Id),
                _context.Products.Count(p => p.WorkspaceId == w.Id),
                w.FirmId,
                w.Firm != null ? w.Firm.Code : null,
                w.Firm != null ? w.Firm.Name : null,
                w.IsDefault))
            .ToListAsync(cancellationToken);
    }
}
