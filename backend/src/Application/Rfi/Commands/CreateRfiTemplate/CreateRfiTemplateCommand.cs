using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfi.Commands.CreateRfiTemplate;

[RequiresPermission(PermissionCodes.RfiManage)]
public sealed record CreateRfiTemplateCommand(string Name, string? Description, Guid? CategoryId) : IRequest<Guid>;
