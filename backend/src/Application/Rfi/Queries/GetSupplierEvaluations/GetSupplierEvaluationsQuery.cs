using MediatR;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfi.Queries.GetSupplierEvaluations;

public sealed record GetSupplierEvaluationsQuery(Guid RfiProcessId) : IRequest<IReadOnlyCollection<SupplierEvaluationDto>>, IRfiScopedRequest;
