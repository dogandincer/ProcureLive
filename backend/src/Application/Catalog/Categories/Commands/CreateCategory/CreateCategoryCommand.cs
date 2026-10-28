using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Categories.Commands.CreateCategory;

[RequiresPermission(PermissionCodes.CatalogManage)]
public sealed record CreateCategoryCommand(string Name, string Code, Guid? ParentCategoryId) : IRequest<Guid>;
