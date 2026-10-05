using MediatR;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Companies;

namespace ProcureLive.Application.Companies.Queries.GetCompaniesList;

public sealed record GetCompaniesListQuery(
    string? SearchTerm,
    string? CompanyType,
    string? Status,
    Guid? SectorId,
    Guid? WorkspaceId,
    int Page,
    int PageSize) : IRequest<PagedResult<CompanyDto>>;
