using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Workspaces;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Commands.CreateRfqProcess;

public sealed class CreateRfqProcessCommandHandler : IRequestHandler<CreateRfqProcessCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IDataScopeService _scope;

    public CreateRfqProcessCommandHandler(IApplicationDbContext context, IDataScopeService scope)
    {
        _context = context;
        _scope = scope;
    }

    public async Task<Guid> Handle(CreateRfqProcessCommand request, CancellationToken cancellationToken)
    {
        var process = new RfqProcess
        {
            Title = request.Title,
            OwnerCompanyId = request.OwnerCompanyId,
            // Verilmezse firmanın varsayılan işyeri (satınalmacı için yetkili işyerlerinden).
            WorkspaceId = await FirmWorkspaceRules.ResolveAsync(
                _context, request.OwnerCompanyId, request.WorkspaceId, await _scope.GetAsync(cancellationToken), cancellationToken),
            CurrencyId = request.CurrencyId,
            Status = ProcessStatus.Draft,
            DueDate = request.DueDate
        };

        _context.RfqProcesses.Add(process);
        await _context.SaveChangesAsync(cancellationToken);

        return process.Id;
    }
}
