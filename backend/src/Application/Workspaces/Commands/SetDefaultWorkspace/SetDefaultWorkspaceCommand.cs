using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Workspaces;

namespace ProcureLive.Application.Workspaces.Commands.SetDefaultWorkspace;

/// <summary>İşyerini kendi firmasının varsayılanı yapar (firmanın diğer işyerlerinden varsayılan kalkar).</summary>
public sealed record SetDefaultWorkspaceCommand(Guid Id) : IRequest;

public sealed class SetDefaultWorkspaceCommandHandler : IRequestHandler<SetDefaultWorkspaceCommand>
{
    private readonly IApplicationDbContext _context;

    public SetDefaultWorkspaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SetDefaultWorkspaceCommand request, CancellationToken cancellationToken)
    {
        Workspace workspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Workspace), request.Id);

        await WorkspaceRules.ApplyDefaultAsync(_context, workspace, requested: true, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
