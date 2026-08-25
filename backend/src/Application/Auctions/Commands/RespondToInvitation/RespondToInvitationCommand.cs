using MediatR;

namespace ProcureLive.Application.Auctions.Commands.RespondToInvitation;

/// <summary>
/// Not gated by AuctionAccessRequirement on purpose: a freshly-invited Supplier does not yet satisfy
/// that policy (only Accepted/Participating/Won/Lost do). Ownership is instead checked in the handler
/// by matching the invitation row to the caller's own CompanyId.
/// </summary>
public sealed record RespondToInvitationCommand(Guid AuctionId, bool Accept, string? DeclineReason) : IRequest;
