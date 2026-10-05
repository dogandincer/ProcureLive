using MediatR;

namespace ProcureLive.Application.Companies.Commands.AssignSectors;

public sealed record AssignSectorsCommand(Guid CompanyId, IReadOnlyCollection<Guid> SectorIds) : IRequest;
