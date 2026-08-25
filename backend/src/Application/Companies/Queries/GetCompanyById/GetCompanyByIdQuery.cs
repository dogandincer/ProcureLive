using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Application.Companies.Queries.GetCompanyById;

public sealed record GetCompanyByIdQuery(Guid Id) : IRequest<CompanyDetailDto>, IRequireResourceAuthorization
{
    public string PolicyName => Policies.CompanyAccess;

    public async Task<object?> LoadResourceAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await context.Companies.FindAsync(new object?[] { Id }, cancellationToken);
    }
}
