using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Users;
using ProcureLive.Domain.Entities.Workspaces;

namespace ProcureLive.Application.Users.Access;

public sealed record GetUserWorkspaceAccessQuery(Guid UserId) : IRequest<UserWorkspaceAccessDto>;

public sealed class GetUserWorkspaceAccessQueryHandler : IRequestHandler<GetUserWorkspaceAccessQuery, UserWorkspaceAccessDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserWorkspaceAccessQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserWorkspaceAccessDto> Handle(GetUserWorkspaceAccessQuery request, CancellationToken cancellationToken)
    {
        List<Guid> ids = await _context.UserWorkspaceAccesses
            .Where(a => a.UserId == request.UserId)
            .Select(a => a.WorkspaceId)
            .ToListAsync(cancellationToken);
        return new UserWorkspaceAccessDto(request.UserId, ids);
    }
}

/// <summary>
/// Satınalmacının işyeri yetkilerini verilen listeyle değiştirir (Admin). Firma yetkisi seçilen işyerlerinin
/// firmalarından türer; Admin'e yetki verilmez (zaten tümüne yetkilidir), tedarikçiye verilmez.
/// </summary>
public sealed record SetUserWorkspaceAccessCommand(Guid UserId, IReadOnlyList<Guid> WorkspaceIds) : IRequest;

public sealed class SetUserWorkspaceAccessCommandHandler : IRequestHandler<SetUserWorkspaceAccessCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;

    public SetUserWorkspaceAccessCommandHandler(
        IApplicationDbContext context, IIdentityService identityService, ICurrentUserService currentUser, IDateTime dateTime)
    {
        _context = context;
        _identityService = identityService;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task Handle(SetUserWorkspaceAccessCommand request, CancellationToken cancellationToken)
    {
        ApplicationUserInfo user = await _identityService.FindByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("ApplicationUser", request.UserId);

        if (!user.Roles.Contains(Roles.Buyer) || user.Roles.Contains(Roles.Admin))
        {
            throw new ConflictException("Firma/işyeri yetkisi yalnızca Satınalmacı kullanıcılara verilir.");
        }

        List<Guid> wanted = request.WorkspaceIds.Distinct().ToList();
        int existingCount = await _context.Workspaces.CountAsync(w => wanted.Contains(w.Id), cancellationToken);
        if (existingCount != wanted.Count)
        {
            throw new ConflictException("Seçilen işyerlerinden biri bulunamadı.");
        }

        List<UserWorkspaceAccess> current = await _context.UserWorkspaceAccesses
            .Where(a => a.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        foreach (UserWorkspaceAccess removed in current.Where(a => !wanted.Contains(a.WorkspaceId)))
        {
            _context.UserWorkspaceAccesses.Remove(removed);
        }

        DateTime now = _dateTime.UtcNow;
        foreach (Guid workspaceId in wanted.Where(id => current.All(a => a.WorkspaceId != id)))
        {
            _context.UserWorkspaceAccesses.Add(new UserWorkspaceAccess
            {
                UserId = request.UserId,
                WorkspaceId = workspaceId,
                GrantedAt = now,
                GrantedByUserId = _currentUser.UserId,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
