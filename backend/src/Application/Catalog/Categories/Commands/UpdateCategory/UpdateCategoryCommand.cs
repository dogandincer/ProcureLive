using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Categories.Commands.UpdateCategory;

[RequiresPermission(PermissionCodes.CatalogManage)]
public sealed record UpdateCategoryCommand(Guid Id, string Name, string Code, Guid? ParentCategoryId) : IRequest;
