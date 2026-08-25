using MediatR;
using ProcureLive.Contracts.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetSupplierEvaluations;

public sealed record GetSupplierEvaluationsQuery(Guid RfiProcessId) : IRequest<IReadOnlyCollection<SupplierEvaluationDto>>;
