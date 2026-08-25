using MediatR;

namespace ProcureLive.Application.Rfi.Commands.RespondToRfiInvitation;

public sealed record RespondToRfiInvitationCommand(Guid RfiProcessId, bool Accept, string? DeclineReason) : IRequest;
