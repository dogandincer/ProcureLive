using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Sectors.Commands.DeleteSector;

[RequiresPermission(PermissionCodes.CatalogManage)]
public sealed record DeleteSectorCommand(Guid Id) : IRequest;
