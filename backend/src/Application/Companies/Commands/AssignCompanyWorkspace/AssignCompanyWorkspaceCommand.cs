using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Commands.AssignCompanyWorkspace;

public sealed record AssignCompanyWorkspaceCommand(Guid CompanyId, Guid? WorkspaceId) : IRequest, ICompanyScopedRequest, IWorkspaceTargetRequest;
