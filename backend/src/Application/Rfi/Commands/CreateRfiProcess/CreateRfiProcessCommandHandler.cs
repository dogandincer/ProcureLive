using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Commands.CreateRfiProcess;

public sealed class CreateRfiProcessCommandHandler : IRequestHandler<CreateRfiProcessCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateRfiProcessCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateRfiProcessCommand request, CancellationToken cancellationToken)
    {
        var process = new RfiProcess
        {
            RfiTemplateId = request.RfiTemplateId,
            Title = request.Title,
            OwnerCompanyId = request.OwnerCompanyId,
            Status = ProcessStatus.Draft,
            DueDate = request.DueDate
        };

        _context.RfiProcesses.Add(process);
        await _context.SaveChangesAsync(cancellationToken);

        return process.Id;
    }
}
