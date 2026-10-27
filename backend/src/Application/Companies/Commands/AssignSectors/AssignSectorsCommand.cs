using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Commands.AssignSectors;

public sealed record AssignSectorsCommand(Guid CompanyId, IReadOnlyCollection<Guid> SectorIds) : IRequest, ICompanyScopedRequest;
