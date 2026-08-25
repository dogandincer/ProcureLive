using MediatR;

namespace ProcureLive.Application.Catalog.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest;
