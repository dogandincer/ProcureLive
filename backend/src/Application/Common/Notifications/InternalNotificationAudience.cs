using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Common;

namespace ProcureLive.Application.Common.Notifications;

/// <summary>
/// İç kullanıcı bildirimlerinin alıcıları: sürecin işyerinde (işyeri yoksa sahip firmanın herhangi bir işyerinde) yetkili
/// aktif iç kullanıcılar + sorumlu/oluşturan kişi. Kimse kalmazsa bildirim kaybolmasın diye aktif Admin'lere gider.
/// Tüm iç kullanıcılara yayın yapılmaz: kapsam dışı satınalmacı başka işyerinin süreç ve tedarikçi adlarını görmemeli.
/// </summary>
public sealed class InternalNotificationAudience
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private Dictionary<Guid, ApplicationUserInfo>? _activeInternal;

    public InternalNotificationAudience(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<IReadOnlyList<Guid>> ForScopeAsync(
        Guid? workspaceId, Guid ownerFirmId, Guid? responsibleUserId, string? createdByUserName, CancellationToken cancellationToken)
    {
        List<Guid> ids = workspaceId is Guid ws
            ? await _context.UserWorkspaceAccesses.Where(a => a.WorkspaceId == ws).Select(a => a.UserId).Distinct().ToListAsync(cancellationToken)
            : await _context.UserWorkspaceAccesses
                .Where(a => _context.Workspaces.Any(w => w.Id == a.WorkspaceId && w.FirmId == ownerFirmId))
                .Select(a => a.UserId).Distinct().ToListAsync(cancellationToken);

        if (responsibleUserId is Guid responsible) ids.Add(responsible);
        if (!string.IsNullOrWhiteSpace(createdByUserName)
            && await _identityService.FindByEmailAsync(createdByUserName, cancellationToken) is { } creator)
        {
            ids.Add(creator.Id);
        }

        Dictionary<Guid, ApplicationUserInfo> active = await ActiveInternalAsync(cancellationToken);
        List<Guid> result = ids.Distinct().Where(active.ContainsKey).ToList();
        return result.Count > 0 ? result : await AdminsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> AdminsAsync(CancellationToken cancellationToken) =>
        (await ActiveInternalAsync(cancellationToken)).Values.Where(u => u.Roles.Contains(Roles.Admin)).Select(u => u.Id).ToList();

    private async Task<Dictionary<Guid, ApplicationUserInfo>> ActiveInternalAsync(CancellationToken cancellationToken)
    {
        if (_activeInternal is not null) return _activeInternal;
        PagedResult<ApplicationUserInfo> users = await _identityService.ListUsersAsync(
            null, null, 1, 1000, [Roles.Admin, Roles.Buyer], cancellationToken);
        _activeInternal = users.Items.Where(u => u.IsActive).ToDictionary(u => u.Id);
        return _activeInternal;
    }
}
