using MediatR;

namespace ProcureLive.Application.Rfi.Commands.CreateRfiProcess;

public sealed record CreateRfiProcessCommand(Guid RfiTemplateId, string Title, Guid OwnerCompanyId, DateTime DueDate) : IRequest<Guid>;
