using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Application.Rfi.Commands.EvaluateSupplier;

public sealed class EvaluateSupplierCommandHandler : IRequestHandler<EvaluateSupplierCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public EvaluateSupplierCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<Guid> Handle(EvaluateSupplierCommand request, CancellationToken cancellationToken)
    {
        SupplierEvaluation? existing = await _context.SupplierEvaluations.FirstOrDefaultAsync(
            e => e.RfiProcessId == request.RfiProcessId && e.CompanyId == request.CompanyId, cancellationToken);

        DateTime now = _dateTime.UtcNow;
        Guid evaluatorId = _currentUserService.UserId ?? Guid.Empty;

        if (existing is not null)
        {
            existing.TotalScore = request.TotalScore;
            existing.Comments = request.Comments;
            existing.EvaluatedByUserId = evaluatorId;
            existing.EvaluatedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var evaluation = new SupplierEvaluation
        {
            RfiProcessId = request.RfiProcessId,
            CompanyId = request.CompanyId,
            EvaluatedByUserId = evaluatorId,
            TotalScore = request.TotalScore,
            Comments = request.Comments,
            EvaluatedAt = now
        };

        _context.SupplierEvaluations.Add(evaluation);
        await _context.SaveChangesAsync(cancellationToken);

        return evaluation.Id;
    }
}
