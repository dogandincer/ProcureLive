using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.AddProductImage;

[RequiresPermission(PermissionCodes.ProductsManage)]
public sealed record AddProductImageCommand(Guid ProductId, string Url, bool IsPrimary, int SortOrder) : IRequest<Guid>, IProductScopedRequest;
