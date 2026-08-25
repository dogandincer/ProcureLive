using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.RespondToInvitation;

public sealed class RespondToInvitationCommandHandler : IRequestHandler<RespondToInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public RespondToInvitationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task Handle(RespondToInvitationCommand request, CancellationToken cancellationToken)
    {
        Guid? companyId = _currentUserService.CompanyId;

        if (companyId is null)
        {
            throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
        }

        AuctionInvitation? invitation = await _context.AuctionInvitations
            .FirstOrDefaultAsync(i => i.AuctionId == request.AuctionId && i.CompanyId == companyId.Value, cancellationToken);

        if (invitation is null)
        {
            throw new NotFoundException(nameof(AuctionInvitation), request.AuctionId);
        }

        if (invitation.Status != InvitationStatus.Invited)
        {
            throw new ConflictException("Bu davete zaten yanıt verilmiş.");
        }

        DateTime now = _dateTime.UtcNow;
        Guid userId = _currentUserService.UserId ?? Guid.Empty;

        invitation.Status = request.Accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        invitation.RespondedAt = now;

        _context.InvitationResponseLogs.Add(new InvitationResponseLog
        {
            SourceType = InvitationSourceType.Auction,
            SourceId = request.AuctionId,
            CompanyId = companyId.Value,
            UserId = userId,
            ResponseType = request.Accept ? InvitationResponseType.Accepted : InvitationResponseType.Declined,
            Reason = request.Accept ? null : request.DeclineReason,
            RespondedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
