using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.UpdateProduct;

[RequiresPermission(PermissionCodes.ProductsManage)]
public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid CategoryId,
    UnitOfMeasure UnitOfMeasure,
    string? Brand,
    Guid? DefaultCurrencyId,
    bool IsActive) : IRequest, IProductScopedRequest
{
    Guid IProductScopedRequest.ProductId => Id;
}
