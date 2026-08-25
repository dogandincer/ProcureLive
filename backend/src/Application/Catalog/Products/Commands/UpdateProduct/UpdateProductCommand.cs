using MediatR;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Catalog.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    UnitOfMeasure UnitOfMeasure,
    string? Brand,
    Guid? DefaultCurrencyId,
    bool IsActive) : IRequest;
