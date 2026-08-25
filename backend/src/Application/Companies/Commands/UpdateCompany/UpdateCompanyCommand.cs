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
    string? Website) : IRequest, IRequireResourceAuthorization
{
    public string PolicyName => Policies.CompanyAccess;

    public async Task<object?> LoadResourceAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await context.Companies.FindAsync(new object?[] { Id }, cancellationToken);
    }
}
