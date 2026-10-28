using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Commands.DeleteCompany;

[RequiresPermission(PermissionCodes.CompaniesManage)]
public sealed record DeleteCompanyCommand(Guid Id) : IRequest, ICompanyScopedRequest
{
    Guid ICompanyScopedRequest.CompanyId => Id;
}
