using MediatR;

namespace ProcureLive.Application.Sectors.Commands.DeleteSector;

public sealed record DeleteSectorCommand(Guid Id) : IRequest;
