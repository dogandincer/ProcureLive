using MediatR;
using ProcureLive.Contracts.Workspaces;

namespace ProcureLive.Application.Workspaces.Queries.GetWorkspacesList;

public sealed record GetWorkspacesListQuery(bool? ActiveOnly) : IRequest<List<WorkspaceDto>>;
