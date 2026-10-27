using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Commands.AddContactPerson;

public sealed record AddContactPersonCommand(
    Guid CompanyId,
    string FirstName,
    string LastName,
    string? Title,
    string Email,
    string? Phone,
    bool IsPrimary) : IRequest<Guid>, ICompanyScopedRequest;
