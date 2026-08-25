using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Commands.UpdateCompany;

public sealed class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCompanyCommandHandler(ICompanyRepository companyRepository, IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        Company? company = await _companyRepository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
        {
            throw new NotFoundException(nameof(Company), request.Id);
        }

        company.Name = request.Name;
        company.TaxOffice = request.TaxOffice;
        company.Status = request.Status;
        company.Website = request.Website;

        _companyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
