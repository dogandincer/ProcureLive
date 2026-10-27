using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Catalog.Products.Commands.AssignProductWorkspace;

public sealed record AssignProductWorkspaceCommand(Guid ProductId, Guid? WorkspaceId) : IRequest, IProductScopedRequest, IWorkspaceTargetRequest;
