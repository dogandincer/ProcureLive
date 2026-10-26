using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public DeleteProductCommandHandler(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.Id);
        }

        bool inUse = await _context.AuctionItems.AnyAsync(i => i.ProductId == request.Id, cancellationToken)
                  || await _context.RfqLineItems.AnyAsync(l => l.ProductId == request.Id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Bir ihale veya RFQ kaleminde kullanılan ürün silinemez.");
        }

        product.IsDeleted = true;
        product.DeletedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
