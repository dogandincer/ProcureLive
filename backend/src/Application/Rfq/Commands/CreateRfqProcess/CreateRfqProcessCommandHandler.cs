using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Commands.CreateRfqProcess;

public sealed class CreateRfqProcessCommandHandler : IRequestHandler<CreateRfqProcessCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateRfqProcessCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateRfqProcessCommand request, CancellationToken cancellationToken)
    {
        var process = new RfqProcess
        {
            Title = request.Title,
            OwnerCompanyId = request.OwnerCompanyId,
            CurrencyId = request.CurrencyId,
            Status = ProcessStatus.Draft,
            DueDate = request.DueDate
        };

        _context.RfqProcesses.Add(process);
        await _context.SaveChangesAsync(cancellationToken);

        return process.Id;
    }
}
