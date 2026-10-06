using MediatR;

namespace ProcureLive.Application.Catalog.Products.Commands.AssignProductWorkspace;

public sealed record AssignProductWorkspaceCommand(Guid ProductId, Guid? WorkspaceId) : IRequest;
