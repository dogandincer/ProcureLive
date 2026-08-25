using MediatR;

namespace ProcureLive.Application.Rfi.Commands.CreateRfiTemplate;

public sealed record CreateRfiTemplateCommand(string Name, string? Description, Guid? CategoryId) : IRequest<Guid>;
