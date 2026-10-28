using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.CreateProduct;

[RequiresPermission(PermissionCodes.ProductsManage)]
public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    Guid CategoryId,
    UnitOfMeasure UnitOfMeasure,
    string? Brand,
    Guid? DefaultCurrencyId,
    Guid? WorkspaceId) : IRequest<Guid>, IWorkspaceTargetRequest;
