using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Common.Security;

/// <summary>
/// İç kullanıcının veri kapsamı. Admin her şeyi görür (IsUnrestricted). Satınalmacı yalnızca Admin'in kendisine
/// atadığı işyerlerini ve bu işyerlerinin firmalarını görür. Tüm iç listeler ve ScopeAuthorizationBehavior bunu kullanır.
/// </summary>
public sealed class DataScope
{
    public static readonly DataScope Unrestricted = new(true, new HashSet<Guid>(), new HashSet<Guid>());

    public DataScope(bool isUnrestricted, IReadOnlySet<Guid> workspaceIds, IReadOnlySet<Guid> firmIds)
    {
        IsUnrestricted = isUnrestricted;
        WorkspaceIds = workspaceIds;
        FirmIds = firmIds;
    }

    public bool IsUnrestricted { get; }

    public IReadOnlySet<Guid> WorkspaceIds { get; }

    public IReadOnlySet<Guid> FirmIds { get; }

    public bool HasFirm(Guid firmId) => IsUnrestricted || FirmIds.Contains(firmId);

    public bool HasWorkspace(Guid? workspaceId) => IsUnrestricted || (workspaceId is Guid id && WorkspaceIds.Contains(id));
}

public interface IDataScopeService
{
    Task<DataScope> GetAsync(CancellationToken cancellationToken);
}

public sealed class DataScopeService : IDataScopeService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private DataScope? _cached;

    public DataScopeService(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DataScope> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is not null) return _cached;

        if (_currentUser.IsInRole(Roles.Admin) || _currentUser.UserId is not Guid userId)
        {
            return _cached = DataScope.Unrestricted;
        }

        var rows = await _context.UserWorkspaceAccesses
            .Where(a => a.UserId == userId)
            .Select(a => new { a.WorkspaceId, a.Workspace.FirmId })
            .ToListAsync(cancellationToken);

        return _cached = new DataScope(
            false,
            rows.Select(r => r.WorkspaceId).ToHashSet(),
            rows.Where(r => r.FirmId.HasValue).Select(r => r.FirmId!.Value).ToHashSet());
    }
}

/// <summary>Kapsam işaretleri: özellik adları istek kayıtlarındaki mevcut alanlarla aynıdır (çoğu örtük uygular).</summary>
public interface IAuctionScopedRequest { Guid AuctionId { get; } }

public interface IRfiScopedRequest { Guid RfiProcessId { get; } }

public interface IRfqScopedRequest { Guid RfqProcessId { get; } }

public interface ICompanyScopedRequest { Guid CompanyId { get; } }

public interface IProductScopedRequest { Guid ProductId { get; } }

/// <summary>Oluşturma istekleri: sahibi olacak firma kullanıcının kapsamında olmalı.</summary>
public interface IOwnerFirmScopedRequest { Guid OwnerCompanyId { get; } }

/// <summary>Hedef işyeri kullanıcının kapsamında olmalı (cari/ürün oluşturma, işyeri atama).</summary>
public interface IWorkspaceTargetRequest { Guid? WorkspaceId { get; } }
