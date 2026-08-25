using MediatR;

namespace ProcureLive.Application.Auth.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest;
