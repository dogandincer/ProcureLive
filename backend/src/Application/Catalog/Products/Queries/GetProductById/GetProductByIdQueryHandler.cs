using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Mappings;
using ProcureLive.Contracts.Catalog;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Application.Catalog.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        ProductDto? dto = await _context.Products
            .Where(p => p.Id == request.Id)
            .Select(Projections.Product)
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            throw new NotFoundException(nameof(Product), request.Id);
        }

        return dto;
    }
}
