using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.InviteCompanies;

public sealed class InviteCompaniesCommandHandler : IRequestHandler<InviteCompaniesCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public InviteCompaniesCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task Handle(InviteCompaniesCommand request, CancellationToken cancellationToken)
    {
        Auction? auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction is null)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        if (auction.Status is not (AuctionStatus.Draft or AuctionStatus.Scheduled))
        {
            throw new ConflictException("Yalnızca taslak veya planlanmış ihalelere davet gönderilebilir.");
        }

        List<Guid> alreadyInvited = await _context.AuctionInvitations
            .Where(i => i.AuctionId == request.AuctionId && request.CompanyIds.Contains(i.CompanyId))
            .Select(i => i.CompanyId)
            .ToListAsync(cancellationToken);

        DateTime now = _dateTime.UtcNow;
        Guid invitedByUserId = _currentUserService.UserId ?? Guid.Empty;

        foreach (Guid companyId in request.CompanyIds.Except(alreadyInvited))
        {
            _context.AuctionInvitations.Add(new AuctionInvitation
            {
                AuctionId = request.AuctionId,
                CompanyId = companyId,
                InvitedByUserId = invitedByUserId,
                Status = InvitationStatus.Invited,
                InvitedAt = now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
