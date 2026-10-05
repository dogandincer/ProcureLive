using MediatR;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Catalog.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    Guid CategoryId,
    UnitOfMeasure UnitOfMeasure,
    string? Brand,
    Guid? DefaultCurrencyId,
    Guid? WorkspaceId) : IRequest<Guid>;
