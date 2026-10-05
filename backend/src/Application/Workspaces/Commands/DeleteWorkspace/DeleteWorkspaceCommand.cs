using MediatR;

namespace ProcureLive.Application.Workspaces.Commands.DeleteWorkspace;

public sealed record DeleteWorkspaceCommand(Guid Id) : IRequest;
