using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Products.Commands.RemoveProductImage;

public sealed class RemoveProductImageCommandHandler : IRequestHandler<RemoveProductImageCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveProductImageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RemoveProductImageCommand request, CancellationToken cancellationToken)
    {
        ProductImage? image = await _context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == request.ImageId && i.ProductId == request.ProductId, cancellationToken);

        if (image is null)
        {
            throw new NotFoundException(nameof(ProductImage), request.ImageId);
        }

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
