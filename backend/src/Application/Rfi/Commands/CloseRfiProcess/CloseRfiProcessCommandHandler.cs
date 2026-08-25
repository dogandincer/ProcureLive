using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Commands.CloseRfiProcess;

public sealed class CloseRfiProcessCommandHandler : IRequestHandler<CloseRfiProcessCommand>
{
    private readonly IApplicationDbContext _context;

    public CloseRfiProcessCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(CloseRfiProcessCommand request, CancellationToken cancellationToken)
    {
        RfiProcess? process = await _context.RfiProcesses.FirstOrDefaultAsync(p => p.Id == request.RfiProcessId, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfiProcess), request.RfiProcessId);
        }

        if (process.Status is ProcessStatus.Closed or ProcessStatus.Evaluated)
        {
            throw new ConflictException("Bu RFI süreci zaten kapatılmış.");
        }

        bool hasEvaluations = await _context.SupplierEvaluations.AnyAsync(e => e.RfiProcessId == process.Id, cancellationToken);

        process.Status = hasEvaluations ? ProcessStatus.Evaluated : ProcessStatus.Closed;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
