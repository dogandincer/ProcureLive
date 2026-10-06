using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Commands.EvaluateCompany;

public sealed class EvaluateCompanyCommandHandler : IRequestHandler<EvaluateCompanyCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    // Kurumsal ağırlıklar (toplam 1.00). Satın alma önceliklerini yansıtır.
    private const decimal WeightQuality = 0.30m;
    private const decimal WeightDelivery = 0.25m;
    private const decimal WeightPrice = 0.20m;
    private const decimal WeightCommunication = 0.15m;
    private const decimal WeightCompliance = 0.10m;

    public EvaluateCompanyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<Guid> Handle(EvaluateCompanyCommand request, CancellationToken cancellationToken)
    {
        bool companyExists = await _context.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (!companyExists)
            throw new NotFoundException(nameof(Company), request.CompanyId);

        decimal overall =
            request.QualityScore * WeightQuality +
            request.DeliveryScore * WeightDelivery +
            request.PriceScore * WeightPrice +
            request.CommunicationScore * WeightCommunication +
            request.ComplianceScore * WeightCompliance;

        var evaluation = new CompanyEvaluation
        {
            CompanyId = request.CompanyId,
            EvaluatedByUserId = _currentUserService.UserId ?? Guid.Empty,
            QualityScore = request.QualityScore,
            DeliveryScore = request.DeliveryScore,
            CommunicationScore = request.CommunicationScore,
            PriceScore = request.PriceScore,
            ComplianceScore = request.ComplianceScore,
            OverallScore = Math.Round(overall, 2),
            Comments = request.Comments,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "Manual" : request.SourceType,
            SourceProcessId = request.SourceProcessId,
            EvaluatedAt = _dateTime.UtcNow
        };

        _context.CompanyEvaluations.Add(evaluation);
        await _context.SaveChangesAsync(cancellationToken);

        return evaluation.Id;
    }
}
