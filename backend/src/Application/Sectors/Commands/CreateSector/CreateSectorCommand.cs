using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Sectors.Commands.CreateSector;

[RequiresPermission(PermissionCodes.CatalogManage)]
public sealed record CreateSectorCommand(string Name, string? Description) : IRequest<Guid>;
