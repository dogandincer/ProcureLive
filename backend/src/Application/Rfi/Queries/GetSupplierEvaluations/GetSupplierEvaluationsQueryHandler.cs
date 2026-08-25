using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetSupplierEvaluations;

public sealed class GetSupplierEvaluationsQueryHandler
    : IRequestHandler<GetSupplierEvaluationsQuery, IReadOnlyCollection<SupplierEvaluationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSupplierEvaluationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<SupplierEvaluationDto>> Handle(
        GetSupplierEvaluationsQuery request,
        CancellationToken cancellationToken)
    {
        List<SupplierEvaluation> evaluations = await _context.SupplierEvaluations
            .Include(e => e.Company)
            .Where(e => e.RfiProcessId == request.RfiProcessId)
            .OrderByDescending(e => e.TotalScore)
            .ToListAsync(cancellationToken);

        return evaluations.Select(e => new SupplierEvaluationDto(
            e.Id, e.CompanyId, e.Company.Name, e.TotalScore, e.Comments, e.EvaluatedAt)).ToList();
    }
}
