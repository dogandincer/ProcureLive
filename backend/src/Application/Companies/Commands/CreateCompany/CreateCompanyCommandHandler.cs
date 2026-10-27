using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IDataScopeService _scopeService;

    public CreateCompanyCommandHandler(IApplicationDbContext context, IIdentityService identityService, IDataScopeService scopeService)
    {
        _context = context;
        _identityService = identityService;
        _scopeService = scopeService;
    }

    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        string? userEmail = string.IsNullOrWhiteSpace(request.UserEmail) ? null : request.UserEmail.Trim().ToLowerInvariant();

        // Kimlik kaydı ayrı yazıldığından e-posta çakışması cari kaydedilmeden önce yakalanır.
        if (userEmail is not null && await _identityService.FindByEmailAsync(userEmail, cancellationToken) is not null)
            throw new ConflictException("Bu e-posta ile zaten bir kullanıcı var.", ErrorCodes.AccountExists);

        var company = new Company
        {
            Code = await CompanyRules.ResolveCodeAsync(_context, request.Code, null, cancellationToken),
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

        _context.Companies.Add(company);

        if (request.SectorIds is { Count: > 0 })
        {
            List<Guid> sectorIds = await _context.Sectors
                .Where(s => request.SectorIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(cancellationToken);
            foreach (Guid sectorId in sectorIds)
                _context.CompanySectors.Add(new CompanySector { CompanyId = company.Id, SectorId = sectorId });
        }

        if (request.VisibleWorkspaceIds is { Count: > 0 })
        {
            DataScope scope = await _scopeService.GetAsync(cancellationToken);
            await CompanyRules.SetVisibleWorkspacesAsync(_context, company, request.VisibleWorkspaceIds, scope, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (userEmail is not null)
        {
            var result = await _identityService.CreateUserAsync(
                userEmail, request.UserPassword!, request.UserFirstName!.Trim(), request.UserLastName!.Trim(),
                Roles.Supplier, company.Id, cancellationToken);

            if (!result.Succeeded)
                throw new ConflictException("Cari oluşturuldu ancak kullanıcı açılamadı: " + string.Join("; ", result.Errors));
        }

        return company.Id;
    }
}
