using MediatR;
using ProcureLive.Contracts.Companies;

namespace ProcureLive.Application.Companies.Queries.GetSupplierScoreboard;

public sealed record GetSupplierScoreboardQuery(int TopCount = 10) : IRequest<SupplierScoreboardDto>;
