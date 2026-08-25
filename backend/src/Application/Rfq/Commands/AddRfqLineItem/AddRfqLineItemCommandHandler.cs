using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Commands.AddRfqLineItem;

public sealed class AddRfqLineItemCommandHandler : IRequestHandler<AddRfqLineItemCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddRfqLineItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddRfqLineItemCommand request, CancellationToken cancellationToken)
    {
        RfqProcess? process = await _context.RfqProcesses.FirstOrDefaultAsync(p => p.Id == request.RfqProcessId, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfqProcess), request.RfqProcessId);
        }

        if (process.Status != ProcessStatus.Draft)
        {
            throw new ConflictException("Sadece taslak (Draft) durumundaki RFQ süreçlerine kalem eklenebilir.");
        }

        var lineItem = new RfqLineItem
        {
            RfqProcessId = request.RfqProcessId,
            ProductId = request.ProductId,
            Description = request.Description,
            Quantity = request.Quantity,
            UnitOfMeasure = request.UnitOfMeasure,
            SortOrder = request.SortOrder
        };

        _context.RfqLineItems.Add(lineItem);
        await _context.SaveChangesAsync(cancellationToken);

        return lineItem.Id;
    }
}
