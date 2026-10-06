using MediatR;

namespace ProcureLive.Application.Companies.Commands.AssignCompanyWorkspace;

public sealed record AssignCompanyWorkspaceCommand(Guid CompanyId, Guid? WorkspaceId) : IRequest;
