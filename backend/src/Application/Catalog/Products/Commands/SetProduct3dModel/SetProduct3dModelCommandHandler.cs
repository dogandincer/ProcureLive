using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Products.Commands.SetProduct3dModel;

public sealed class SetProduct3dModelCommandHandler : IRequestHandler<SetProduct3dModelCommand>
{
    private readonly IApplicationDbContext _context;

    public SetProduct3dModelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SetProduct3dModelCommand request, CancellationToken cancellationToken)
    {
        Product? product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
            throw new NotFoundException(nameof(Product), request.ProductId);

        product.Model3dUrl = request.Url;
        product.Model3dSource = "Uploaded";

        await _context.SaveChangesAsync(cancellationToken);
    }
}
