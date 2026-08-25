using MediatR;
using ProcureLive.Contracts.Auth;

namespace ProcureLive.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? IpAddress) : IRequest<AuthResultDto>;
