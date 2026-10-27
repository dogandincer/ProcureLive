using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Guid>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;

    public CreateCompanyCommandHandler(ICompanyRepository companyRepository, IUnitOfWork unitOfWork, IApplicationDbContext context)
    {
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = new Company
        {
            Name = request.Name,
            TaxNumber = request.TaxNumber,
            TaxOffice = request.TaxOffice,
            CompanyType = request.CompanyType,
            Website = request.Website,
            WorkspaceId = request.WorkspaceId,
            TradeName = request.TradeName,
            MersisNo = request.MersisNo,
            Phone = request.Phone,
            Email = request.Email,
            FoundationYear = request.FoundationYear,
            EmployeeCount = request.EmployeeCount,
            Description = request.Description,
            Status = CompanyStatus.PendingApproval
        };

        // Internal cari = Firma; firmalarda kod zorunlu olduğundan bu yoldan açılan firmaya da kod üretilir.
        if (company.CompanyType == CompanyType.Internal)
        {
            company.Code = await Firms.FirmRules.NextCodeAsync(_context, cancellationToken);
        }

        await _companyRepository.AddAsync(company, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return company.Id;
    }
}
