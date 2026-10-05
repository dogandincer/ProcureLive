using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Commands.AssignSectors;

public sealed class AssignSectorsCommandHandler : IRequestHandler<AssignSectorsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public AssignSectorsCommandHandler(IApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignSectorsCommand request, CancellationToken cancellationToken)
    {
        bool companyExists = await _context.Companies
            .AnyAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (!companyExists)
        {
            throw new NotFoundException(nameof(Company), request.CompanyId);
        }

        // Remove all existing sector assignments for this company
        List<CompanySector> existing = await _context.CompanySectors
            .Where(cs => cs.CompanyId == request.CompanyId)
            .ToListAsync(cancellationToken);

        _context.CompanySectors.RemoveRange(existing);

        // Add new assignments
        foreach (Guid sectorId in request.SectorIds)
        {
            await _context.CompanySectors.AddAsync(new CompanySector
            {
                CompanyId = request.CompanyId,
                SectorId = sectorId,
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
