using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Sectors.Commands.UpdateSector;

[RequiresPermission(PermissionCodes.CatalogManage)]
public sealed record UpdateSectorCommand(Guid Id, string Name, string? Description, bool IsActive) : IRequest;
