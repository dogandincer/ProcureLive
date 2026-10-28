using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Common.Security;

/// <summary>
/// İşlem bazlı yetkiler. Admin her şeyi yapar; Satınalmacı yalnızca rolüne verilen yetkileri kullanır (Yetkiler ekranı);
/// tedarikçi uçları ayrı korunur (bu kontrol tedarikçiye uygulanmaz). Varsayılan: tüm yetkiler Satınalmacı'da açık.
/// </summary>
public static class PermissionCodes
{
    public const string AuctionsManage = "auctions.manage";
    public const string AuctionsAward = "auctions.award";
    public const string RfiManage = "rfi.manage";
    public const string RfqManage = "rfq.manage";
    public const string RfqAward = "rfq.award";
    public const string CompaniesManage = "companies.manage";
    public const string ProductsManage = "products.manage";
    public const string CatalogManage = "catalog.manage";
    public const string OrdersManage = "orders.manage";
    public const string QaAnswer = "qa.answer";

    /// <summary>Kod → Türkçe açıklama (Yetkiler ekranında ve hata mesajında görünür).</summary>
    public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>
    {
        [AuctionsManage] = "İhale oluşturma, düzenleme, yayınlama ve davet",
        [AuctionsAward] = "İhalede kazanan seçme",
        [RfiManage] = "RFI şablonu ve süreci yönetme",
        [RfqManage] = "RFQ süreci yönetme",
        [RfqAward] = "RFQ'da kazanan seçme",
        [CompaniesManage] = "Cari (tedarikçi) kartı yönetme",
        [ProductsManage] = "Ürün kartı yönetme",
        [CatalogManage] = "Kategori, sektör, para birimi ve kur tanımları",
        [OrdersManage] = "Sipariş oluşturma ve gönderme",
        [QaAnswer] = "Tedarikçi sorularını yanıtlama ve duyuru",
    };
}

/// <summary>Komutun gerektirdiği yetki. Satınalmacı için PermissionBehavior denetler.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RequiresPermissionAttribute : Attribute
{
    public RequiresPermissionAttribute(string code) => Code = code;

    public string Code { get; }
}

public interface IPermissionService
{
    /// <summary>Geçerli kullanıcının yetki kodları (Admin: tümü, Satınalmacı: rolüne verilenler, tedarikçi: boş).</summary>
    Task<IReadOnlyCollection<string>> GetMyPermissionsAsync(CancellationToken cancellationToken);
}

public sealed class PermissionService : IPermissionService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;
    private IReadOnlyCollection<string>? _cache;

    public PermissionService(IApplicationDbContext context, ICurrentUserService currentUser, IIdentityService identity)
    {
        _context = context;
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<IReadOnlyCollection<string>> GetMyPermissionsAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null) return _cache;
        if (_currentUser.IsInRole(Roles.Admin)) return _cache = PermissionCodes.All.Keys.ToList();
        if (!_currentUser.IsInRole(Roles.Buyer)) return _cache = [];

        Guid? roleId = await _identity.GetRoleIdAsync(Roles.Buyer, cancellationToken);
        _cache = roleId is null
            ? []
            : await _context.RolePermissions.Where(rp => rp.RoleId == roleId).Select(rp => rp.Permission.Code).ToListAsync(cancellationToken);
        return _cache;
    }
}

public sealed class PermissionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly string? Required =
        ((RequiresPermissionAttribute?)Attribute.GetCustomAttribute(typeof(TRequest), typeof(RequiresPermissionAttribute)))?.Code;

    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissions;

    public PermissionBehavior(ICurrentUserService currentUser, IPermissionService permissions)
    {
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (Required is not null && _currentUser.IsInRole(Roles.Buyer) && !_currentUser.IsInRole(Roles.Admin))
        {
            IReadOnlyCollection<string> mine = await _permissions.GetMyPermissionsAsync(cancellationToken);
            if (!mine.Contains(Required))
            {
                throw new ForbiddenAccessException($"Bu işlem için yetkiniz yok: {PermissionCodes.All[Required]}.", ErrorCodes.PermissionDenied);
            }
        }

        return await next();
    }
}

// ---------------------------------------------------------------- Yetkiler ekranı (rol yetkileri)

public sealed record RolePermissionDto(string Code, string Description, bool Granted);

public sealed record GetBuyerPermissionsQuery : IRequest<IReadOnlyList<RolePermissionDto>>;

public sealed class GetBuyerPermissionsQueryHandler : IRequestHandler<GetBuyerPermissionsQuery, IReadOnlyList<RolePermissionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identity;

    public GetBuyerPermissionsQueryHandler(IApplicationDbContext context, IIdentityService identity)
    {
        _context = context;
        _identity = identity;
    }

    public async Task<IReadOnlyList<RolePermissionDto>> Handle(GetBuyerPermissionsQuery request, CancellationToken cancellationToken)
    {
        Guid? roleId = await _identity.GetRoleIdAsync(Roles.Buyer, cancellationToken);
        HashSet<string> granted = (await _context.RolePermissions.Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission.Code).ToListAsync(cancellationToken)).ToHashSet();
        return PermissionCodes.All.Select(kv => new RolePermissionDto(kv.Key, kv.Value, granted.Contains(kv.Key))).ToList();
    }
}

public sealed record SetBuyerPermissionsCommand(IReadOnlyCollection<string> Codes) : IRequest;

public sealed class SetBuyerPermissionsCommandHandler : IRequestHandler<SetBuyerPermissionsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identity;

    public SetBuyerPermissionsCommandHandler(IApplicationDbContext context, IIdentityService identity)
    {
        _context = context;
        _identity = identity;
    }

    public async Task Handle(SetBuyerPermissionsCommand request, CancellationToken cancellationToken)
    {
        if (request.Codes.Any(c => !PermissionCodes.All.ContainsKey(c)))
            throw new ConflictException("Bilinmeyen yetki kodu.");
        Guid roleId = await _identity.GetRoleIdAsync(Roles.Buyer, cancellationToken) ?? throw new NotFoundException("Role", Roles.Buyer);

        var catalog = await _context.Permissions.ToListAsync(cancellationToken);
        var current = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
        var wanted = catalog.Where(p => request.Codes.Contains(p.Code)).Select(p => p.Id).ToHashSet();

        _context.RolePermissions.RemoveRange(current.Where(rp => !wanted.Contains(rp.PermissionId)));
        foreach (Guid pid in wanted.Where(id => current.All(rp => rp.PermissionId != id)))
        {
            _context.RolePermissions.Add(new Domain.Entities.Identity.RolePermission { RoleId = roleId, PermissionId = pid });
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record GetMyPermissionsQuery : IRequest<IReadOnlyCollection<string>>;

public sealed class GetMyPermissionsQueryHandler : IRequestHandler<GetMyPermissionsQuery, IReadOnlyCollection<string>>
{
    private readonly IPermissionService _permissions;

    public GetMyPermissionsQueryHandler(IPermissionService permissions) => _permissions = permissions;

    public Task<IReadOnlyCollection<string>> Handle(GetMyPermissionsQuery request, CancellationToken cancellationToken) =>
        _permissions.GetMyPermissionsAsync(cancellationToken);
}
