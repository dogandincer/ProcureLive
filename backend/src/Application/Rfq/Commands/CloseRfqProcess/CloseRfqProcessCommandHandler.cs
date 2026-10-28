using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Commands.CloseRfqProcess;

public sealed class CloseRfqProcessCommandHandler : IRequestHandler<CloseRfqProcessCommand>
{
    private readonly IApplicationDbContext _context;

    public CloseRfqProcessCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(CloseRfqProcessCommand request, CancellationToken cancellationToken)
    {
        RfqProcess? process = await _context.RfqProcesses.FirstOrDefaultAsync(p => p.Id == request.RfqProcessId, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfqProcess), request.RfqProcessId);
        }

        if (process.Status is ProcessStatus.Closed or ProcessStatus.Awarded or ProcessStatus.Cancelled)
        {
            throw new ConflictException("Bu RFQ süreci zaten kapatılmış.");
        }

        process.Status = ProcessStatus.Closed;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
