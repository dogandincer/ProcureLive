using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Commands.PublishRfqProcess;

public sealed class PublishRfqProcessCommandHandler : IRequestHandler<PublishRfqProcessCommand>
{
    private readonly IApplicationDbContext _context;

    public PublishRfqProcessCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(PublishRfqProcessCommand request, CancellationToken cancellationToken)
    {
        RfqProcess? process = await _context.RfqProcesses
            .Include(p => p.LineItems)
            .Include(p => p.Invitations)
            .FirstOrDefaultAsync(p => p.Id == request.RfqProcessId, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfqProcess), request.RfqProcessId);
        }

        if (process.Status != ProcessStatus.Sent)
        {
            throw new ConflictException("Yayınlamadan önce en az bir Cari davet edilmiş olmalıdır.");
        }

        if (process.LineItems.Count == 0)
        {
            throw new ConflictException("Yayınlamadan önce en az bir kalem eklenmelidir.");
        }

        process.Status = ProcessStatus.InProgress;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
