using MediatR;

namespace ProcureLive.Application.Workspaces.Commands.CreateWorkspace;

public sealed record CreateWorkspaceCommand(string Name, string? Description) : IRequest<Guid>;
