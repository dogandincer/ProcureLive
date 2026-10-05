using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Sectors.Commands.UpdateSector;

public sealed class UpdateSectorCommandHandler : IRequestHandler<UpdateSectorCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSectorCommandHandler(IApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateSectorCommand request, CancellationToken cancellationToken)
    {
        Sector? sector = await _context.Sectors
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (sector is null)
        {
            throw new NotFoundException(nameof(Sector), request.Id);
        }

        sector.Name = request.Name;
        sector.Description = request.Description;
        sector.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
