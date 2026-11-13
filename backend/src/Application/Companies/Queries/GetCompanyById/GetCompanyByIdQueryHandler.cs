using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Mappings;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Queries.GetCompanyById;

public sealed class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetCompanyByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDetailDto> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        CompanyDetailDto? dto = await _context.Companies
            .Where(c => c.Id == request.Id)
            .Select(Projections.CompanyDetail)
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            throw new NotFoundException(nameof(Company), request.Id);
        }

        if (dto.Evaluations.Count > 0)
        {
            dto = dto with
            {
                AverageScore = Math.Round(dto.Evaluations.Average(e => e.OverallScore), 2),
                EvaluationCount = dto.Evaluations.Count
            };
        }

        return dto;
    }
}
