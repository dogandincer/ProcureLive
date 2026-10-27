using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Workspaces;

namespace ProcureLive.Application.Workspaces.Commands.UpdateWorkspace;

public sealed class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWorkspaceCommandHandler(IApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        Workspace? workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (workspace is null)
        {
            throw new NotFoundException(nameof(Workspace), request.Id);
        }

        if (workspace.IsDefault && !request.IsDefault && workspace.FirmId == request.FirmId)
        {
            throw new ConflictException("Varsayılan işyeri kaldırılamaz; önce firmanın başka bir işyerini varsayılan yapın.");
        }

        Guid? previousFirm = workspace.FirmId;
        workspace.Name = request.Name;
        workspace.Description = request.Description;
        workspace.IsActive = request.IsActive;
        workspace.FirmId = request.FirmId;

        await WorkspaceRules.ApplyDefaultAsync(_context, workspace, request.IsDefault, cancellationToken);

        // Varsayılan işyeri başka firmaya taşındıysa eski firmada varsayılan boş kalmasın.
        if (previousFirm is Guid oldFirm && oldFirm != request.FirmId)
        {
            List<Workspace> remaining = await _context.Workspaces
                .Where(w => w.FirmId == oldFirm && w.Id != workspace.Id)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync(cancellationToken);
            if (remaining.Count > 0 && remaining.All(w => !w.IsDefault))
            {
                remaining[0].IsDefault = true;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
