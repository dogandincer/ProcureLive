using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Invitations;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Invitations.Queries;

public sealed record GetInviteLinksQuery(Guid AuctionId) : IRequest<IReadOnlyList<InviteLinkDto>>, IAuctionScopedRequest;

public sealed class GetInviteLinksQueryHandler : IRequestHandler<GetInviteLinksQuery, IReadOnlyList<InviteLinkDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public GetInviteLinksQueryHandler(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<IReadOnlyList<InviteLinkDto>> Handle(GetInviteLinksQuery request, CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;
        var rows = await _context.InvitationTokens
            .Where(t => t.AuctionId == request.AuctionId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new { Token = t, CompanyName = t.Company.Name })
            .ToListAsync(cancellationToken);

        return rows.Select(r => InviteRules.ToDto(r.Token, r.CompanyName, now)).ToList();
    }
}
