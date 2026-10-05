using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Sectors.Commands.DeleteSector;

public sealed class DeleteSectorCommandHandler : IRequestHandler<DeleteSectorCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSectorCommandHandler(IApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSectorCommand request, CancellationToken cancellationToken)
    {
        Sector? sector = await _context.Sectors
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (sector is null)
        {
            throw new NotFoundException(nameof(Sector), request.Id);
        }

        _context.Sectors.Remove(sector);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
