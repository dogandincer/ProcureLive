using MediatR;

namespace ProcureLive.Application.Companies.Commands.DeleteCompany;

public sealed record DeleteCompanyCommand(Guid Id) : IRequest;
