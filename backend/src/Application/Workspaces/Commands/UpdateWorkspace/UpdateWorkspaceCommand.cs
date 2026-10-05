using MediatR;

namespace ProcureLive.Application.Workspaces.Commands.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(Guid Id, string Name, string? Description, bool IsActive) : IRequest;
