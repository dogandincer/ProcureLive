using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfi.Commands.CreateRfiProcess;

public sealed record CreateRfiProcessCommand(Guid RfiTemplateId, string Title, Guid OwnerCompanyId, DateTime DueDate, Guid? WorkspaceId = null)
    : IRequest<Guid>, IOwnerFirmScopedRequest, IWorkspaceTargetRequest;
