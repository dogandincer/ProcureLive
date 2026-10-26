using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Commands.DeleteCompany;

public sealed class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTime _dateTime;
    private readonly IApplicationDbContext _context;

    public DeleteCompanyCommandHandler(ICompanyRepository companyRepository, IUnitOfWork unitOfWork, IDateTime dateTime, IApplicationDbContext context)
    {
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _dateTime = dateTime;
        _context = context;
    }

    public async Task Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        Company? company = await _companyRepository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
        {
            throw new NotFoundException(nameof(Company), request.Id);
        }

        Guid id = request.Id;
        bool inUse =
            await _context.Auctions.AnyAsync(a => a.OwnerCompanyId == id, cancellationToken)
            || await _context.AuctionInvitations.AnyAsync(i => i.CompanyId == id, cancellationToken)
            || await _context.Bids.AnyAsync(b => b.CompanyId == id, cancellationToken)
            || await _context.RfqProcesses.AnyAsync(r => r.OwnerCompanyId == id, cancellationToken)
            || await _context.RfqInvitations.AnyAsync(i => i.CompanyId == id, cancellationToken)
            || await _context.RfqQuotes.AnyAsync(q => q.CompanyId == id, cancellationToken)
            || await _context.RfiProcesses.AnyAsync(r => r.OwnerCompanyId == id, cancellationToken)
            || await _context.RfiInvitations.AnyAsync(i => i.CompanyId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Bir ihale, RFQ veya RFI sürecinde kullanılan (sahip/davetli/teklif veren) cari silinemez.");
        }

        company.IsDeleted = true;
        company.DeletedAt = _dateTime.UtcNow;

        _companyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
