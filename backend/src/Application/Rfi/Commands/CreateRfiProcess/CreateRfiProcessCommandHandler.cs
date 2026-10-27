using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Workspaces;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Commands.CreateRfiProcess;

public sealed class CreateRfiProcessCommandHandler : IRequestHandler<CreateRfiProcessCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IDataScopeService _scope;

    public CreateRfiProcessCommandHandler(IApplicationDbContext context, IDataScopeService scope)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<Guid> Handle(CreateRfiProcessCommand request, CancellationToken cancellationToken)
    {
        var process = new RfiProcess
        {
            RfiTemplateId = request.RfiTemplateId,
            Title = request.Title,
            OwnerCompanyId = request.OwnerCompanyId,
            // Verilmezse firmanın varsayılan işyeri (satınalmacı için yetkili işyerlerinden).
            WorkspaceId = await FirmWorkspaceRules.ResolveAsync(
                _context, request.OwnerCompanyId, request.WorkspaceId, await _scope.GetAsync(cancellationToken), cancellationToken),
            Status = ProcessStatus.Draft,
            DueDate = request.DueDate
        };

        _context.RfiProcesses.Add(process);
        await _context.SaveChangesAsync(cancellationToken);

        return process.Id;
    }
}
