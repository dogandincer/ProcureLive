using MediatR;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Companies.Commands.CreateCompany;

public sealed record CreateCompanyCommand(
    string Name,
    string TaxNumber,
    string TaxOffice,
    CompanyType CompanyType,
    string? Website) : IRequest<Guid>;
