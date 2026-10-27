using MediatR;
using ProcureLive.Contracts.Rfq;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfq.Queries.GetRfqComparisonMatrix;

/// <summary>Admin comparison matrix: every submitted quote line, normalized, grouped by line item for side-by-side review.</summary>
public sealed record GetRfqComparisonMatrixQuery(Guid RfqProcessId) : IRequest<IReadOnlyCollection<RfqComparisonRowDto>>, IRfqScopedRequest;
