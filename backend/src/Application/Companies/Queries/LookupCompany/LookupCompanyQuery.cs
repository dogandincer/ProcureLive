using MediatR;
using ProcureLive.Contracts.Companies;

namespace ProcureLive.Application.Companies.Queries.LookupCompany;

public sealed record LookupCompanyQuery(string Identifier) : IRequest<CompanyLookupResultDto>;
