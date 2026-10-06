using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Companies.Commands.UpdateCompany;

public sealed record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string TaxOffice,
    CompanyStatus Status,
    string? Website,
    string? TradeName = null,
    string? MersisNo = null,
    string? Phone = null,
    string? Email = null,
    int? FoundationYear = null,
    int? EmployeeCount = null,
    string? Description = null) : IRequest, IRequireResourceAuthorization
{
    public string PolicyName => Policies.CompanyAccess;

    public async Task<object?> LoadResourceAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await context.Companies.FindAsync(new object?[] { Id }, cancellationToken);
    }
}
