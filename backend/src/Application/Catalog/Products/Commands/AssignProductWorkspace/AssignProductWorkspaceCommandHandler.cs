using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Entities.Workspaces;

namespace ProcureLive.Application.Catalog.Products.Commands.AssignProductWorkspace;

public sealed class AssignProductWorkspaceCommandHandler : IRequestHandler<AssignProductWorkspaceCommand>
{
    private readonly IApplicationDbContext _context;

    public AssignProductWorkspaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AssignProductWorkspaceCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
            throw new NotFoundException(nameof(Product), request.ProductId);

        if (request.WorkspaceId.HasValue)
        {
            bool workspaceExists = await _context.Workspaces
                .AnyAsync(w => w.Id == request.WorkspaceId.Value, cancellationToken);

            if (!workspaceExists)
                throw new NotFoundException(nameof(Workspace), request.WorkspaceId.Value);
        }

        product.WorkspaceId = request.WorkspaceId;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
