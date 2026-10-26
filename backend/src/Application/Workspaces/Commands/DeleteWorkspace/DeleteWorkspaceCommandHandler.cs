using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Workspaces;

namespace ProcureLive.Application.Workspaces.Commands.DeleteWorkspace;

public sealed class DeleteWorkspaceCommandHandler : IRequestHandler<DeleteWorkspaceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWorkspaceCommandHandler(IApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        Workspace? workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (workspace is null)
        {
            throw new NotFoundException(nameof(Workspace), request.Id);
        }

        bool inUse = await _context.Companies.AnyAsync(c => c.WorkspaceId == request.Id, cancellationToken)
                  || await _context.Products.AnyAsync(p => p.WorkspaceId == request.Id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Cari veya ürün atanmış olan işyeri silinemez. Önce atamaları başka bir işyerine taşıyın veya kaldırın.");
        }

        _context.Workspaces.Remove(workspace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
