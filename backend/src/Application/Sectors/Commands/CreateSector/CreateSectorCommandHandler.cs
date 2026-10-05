using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Sectors.Commands.CreateSector;

public sealed class CreateSectorCommandHandler : IRequestHandler<CreateSectorCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSectorCommandHandler(IApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSectorCommand request, CancellationToken cancellationToken)
    {
        var sector = new Sector
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
        };

        await _context.Sectors.AddAsync(sector, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return sector.Id;
    }
}
