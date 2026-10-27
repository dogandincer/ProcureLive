using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Workspaces;

namespace ProcureLive.Application.Workspaces.Queries.GetWorkspacesList;

public sealed class GetWorkspacesListQueryHandler : IRequestHandler<GetWorkspacesListQuery, List<WorkspaceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkspacesListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkspaceDto>> Handle(GetWorkspacesListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ProcureLive.Domain.Entities.Workspaces.Workspace> query = _context.Workspaces.AsQueryable();

        if (request.ActiveOnly == true)
        {
            query = query.Where(w => w.IsActive);
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
