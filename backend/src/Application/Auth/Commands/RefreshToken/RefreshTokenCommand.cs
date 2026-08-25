using MediatR;
using ProcureLive.Contracts.Auth;

namespace ProcureLive.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken, string? IpAddress) : IRequest<AuthResultDto>;
