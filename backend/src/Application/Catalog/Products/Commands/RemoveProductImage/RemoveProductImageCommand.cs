using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.RemoveProductImage;

[RequiresPermission(PermissionCodes.ProductsManage)]
public sealed record RemoveProductImageCommand(Guid ProductId, Guid ImageId) : IRequest, IProductScopedRequest;
