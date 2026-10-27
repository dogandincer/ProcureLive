using MediatR;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Companies.Commands.UpdateCompany;

public sealed class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateCompanyCommandHandler(
        ICompanyRepository companyRepository, IUnitOfWork unitOfWork, IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        Company? company = await _companyRepository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
        {
            throw new NotFoundException(nameof(Company), request.Id);
        }

        bool isInternalUser = _currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Buyer);

        company.Name = request.Name;
        company.TaxOffice = request.TaxOffice;

        // Durum (onay) ve cari kodu iç kullanıcıya aittir; tedarikçi kendi carisini onaylı yapamaz.
        if (isInternalUser)
        {
            company.Status = request.Status;
            if (company.CompanyType != CompanyType.Internal && request.Code is not null)
                company.Code = await CompanyRules.ResolveCodeAsync(_context, request.Code, company.Id, cancellationToken);
        }

        company.Website = request.Website;
        company.TradeName = request.TradeName;
        company.MersisNo = request.MersisNo;
        company.Phone = request.Phone;
        company.Email = request.Email;
        company.FoundationYear = request.FoundationYear;
        company.EmployeeCount = request.EmployeeCount;
        company.Description = request.Description;

        _companyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
