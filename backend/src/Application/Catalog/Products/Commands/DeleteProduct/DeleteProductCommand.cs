using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.DeleteProduct;

[RequiresPermission(PermissionCodes.ProductsManage)]
public sealed record DeleteProductCommand(Guid Id) : IRequest, IProductScopedRequest
{
    Guid IProductScopedRequest.ProductId => Id;
}
