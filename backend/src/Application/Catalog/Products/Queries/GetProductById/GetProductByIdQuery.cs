using MediatR;
using ProcureLive.Contracts.Catalog;

namespace ProcureLive.Application.Catalog.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
