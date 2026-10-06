using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Workspaces;

namespace ProcureLive.Application.Companies.Commands.AssignCompanyWorkspace;

public sealed class AssignCompanyWorkspaceCommandHandler : IRequestHandler<AssignCompanyWorkspaceCommand>
{
    private readonly IApplicationDbContext _context;

    public AssignCompanyWorkspaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AssignCompanyWorkspaceCommand request, CancellationToken cancellationToken)
    {
        Company? company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company is null)
            throw new NotFoundException(nameof(Company), request.CompanyId);

        if (request.WorkspaceId.HasValue)
        {
            bool workspaceExists = await _context.Workspaces
                .AnyAsync(w => w.Id == request.WorkspaceId.Value, cancellationToken);

            if (!workspaceExists)
                throw new NotFoundException(nameof(Workspace), request.WorkspaceId.Value);
        }

        company.WorkspaceId = request.WorkspaceId;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
