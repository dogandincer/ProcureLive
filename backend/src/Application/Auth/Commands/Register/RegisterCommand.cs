using MediatR;
using ProcureLive.Contracts.Auth;

namespace ProcureLive.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    Guid? CompanyId) : IRequest<AuthResultDto>;
