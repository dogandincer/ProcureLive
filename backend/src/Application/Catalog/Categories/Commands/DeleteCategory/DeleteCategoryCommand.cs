using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Categories.Commands.DeleteCategory;

[RequiresPermission(PermissionCodes.CatalogManage)]
public sealed record DeleteCategoryCommand(Guid Id) : IRequest;
