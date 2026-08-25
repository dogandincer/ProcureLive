using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Products.Commands.AddProductImage;

public sealed class AddProductImageCommandHandler : IRequestHandler<AddProductImageCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddProductImageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddProductImageCommand request, CancellationToken cancellationToken)
    {
        bool productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);

        if (!productExists)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        var image = new ProductImage
        {
            ProductId = request.ProductId,
            Url = request.Url,
            IsPrimary = request.IsPrimary,
            SortOrder = request.SortOrder
        };

        _context.ProductImages.Add(image);
        await _context.SaveChangesAsync(cancellationToken);

        return image.Id;
    }
}
