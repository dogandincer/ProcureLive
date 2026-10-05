using MediatR;

namespace ProcureLive.Application.Sectors.Commands.CreateSector;

public sealed record CreateSectorCommand(string Name, string? Description) : IRequest<Guid>;
