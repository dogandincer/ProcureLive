using MediatR;
using ProcureLive.Contracts.Companies;

namespace ProcureLive.Application.Sectors.Queries.GetSectorsList;

public sealed record GetSectorsListQuery(bool? ActiveOnly) : IRequest<List<SectorDto>>;
