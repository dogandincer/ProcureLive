using MediatR;

namespace ProcureLive.Application.Rfq.Commands.RespondToRfqInvitation;

public sealed record RespondToRfqInvitationCommand(Guid RfqProcessId, bool Accept, string? DeclineReason) : IRequest;
