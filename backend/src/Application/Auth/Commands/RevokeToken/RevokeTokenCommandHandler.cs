using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using RefreshTokenEntity = ProcureLive.Domain.Entities.Identity.RefreshToken;

namespace ProcureLive.Application.Auth.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public RevokeTokenCommandHandler(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        RefreshTokenEntity? token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (token is null || !token.IsActive)
        {
            throw new NotFoundException(nameof(RefreshTokenEntity), request.RefreshToken);
        }

        token.RevokedAt = _dateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
