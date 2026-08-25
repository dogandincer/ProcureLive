using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Commands.DeleteCompany;

public sealed class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTime _dateTime;

    public DeleteCompanyCommandHandler(ICompanyRepository companyRepository, IUnitOfWork unitOfWork, IDateTime dateTime)
    {
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _dateTime = dateTime;
    }

    public async Task Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        Company? company = await _companyRepository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
        {
            throw new NotFoundException(nameof(Company), request.Id);
        }

        company.IsDeleted = true;
        company.DeletedAt = _dateTime.UtcNow;

        _companyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
