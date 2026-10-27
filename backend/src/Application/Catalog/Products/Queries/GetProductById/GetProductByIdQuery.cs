using MediatR;
using ProcureLive.Contracts.Catalog;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>, IProductScopedRequest
{
    Guid IProductScopedRequest.ProductId => Id;
}
