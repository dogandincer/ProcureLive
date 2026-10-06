using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Products.Commands.RemoveProduct3dModel;

public sealed class RemoveProduct3dModelCommandHandler : IRequestHandler<RemoveProduct3dModelCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveProduct3dModelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RemoveProduct3dModelCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
            throw new NotFoundException(nameof(Product), request.ProductId);

        product.Model3dUrl = null;
        product.Model3dSource = null;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
