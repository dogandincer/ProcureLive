using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Commands.AssignCompanyWorkspace;

[RequiresPermission(PermissionCodes.CompaniesManage)]
public sealed record AssignCompanyWorkspaceCommand(Guid CompanyId, Guid? WorkspaceId) : IRequest, ICompanyScopedRequest, IWorkspaceTargetRequest;
