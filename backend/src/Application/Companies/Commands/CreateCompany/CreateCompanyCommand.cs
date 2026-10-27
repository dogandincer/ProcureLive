using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Commands.CreateCompany;

public sealed record CreateCompanyCommand(
    string Name,
    string TaxNumber,
    string TaxOffice,
    CompanyType CompanyType,
    string? Website,
    Guid? WorkspaceId,
    string? TradeName = null,
    string? MersisNo = null,
    string? Phone = null,
    string? Email = null,
    int? FoundationYear = null,
    int? EmployeeCount = null,
    string? Description = null) : IRequest<Guid>, IWorkspaceTargetRequest;
