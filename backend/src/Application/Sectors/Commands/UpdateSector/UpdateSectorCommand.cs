using MediatR;

namespace ProcureLive.Application.Sectors.Commands.UpdateSector;

public sealed record UpdateSectorCommand(Guid Id, string Name, string? Description, bool IsActive) : IRequest;
