using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Behaviors;

/// <summary>
/// Satınalmacının firma/işyeri kapsamını merkezi olarak uygular: kapsam işaretli her istekte hedef kaydın firması
/// (ihale/RFI/RFQ sahibi) veya işyeri (cari/ürün) kullanıcının kapsamında değilse 403. Admin ve tedarikçi bu
/// davranıştan etkilenmez (tedarikçi erişimi SupplierAccessContext ile ayrıca yönetilir). Kayıt yoksa handler 404 döner.
/// </summary>
public sealed class ScopeAuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const string OutOfScope = "Bu kayıt yetkili olduğunuz firma/işyerlerine ait değil.";

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDataScopeService _scopeService;

    public ScopeAuthorizationBehavior(IApplicationDbContext context, ICurrentUserService currentUser, IDataScopeService scopeService)
    {
        _context = context;
        _currentUser = currentUser;
        _scopeService = scopeService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        bool scopedBuyer = _currentUser.IsInRole(Roles.Buyer) && !_currentUser.IsInRole(Roles.Admin);
        if (!scopedBuyer || !IsScoped(request))
        {
            return await next();
        }

        DataScope scope = await _scopeService.GetAsync(cancellationToken);
        if (!await IsAllowedAsync(request, scope, cancellationToken))
        {
            throw new ForbiddenAccessException(OutOfScope);
        }

        return await next();
    }

    private static bool IsScoped(TRequest r) =>
        r is IAuctionScopedRequest or IRfiScopedRequest or IRfqScopedRequest or ICompanyScopedRequest
            or IProductScopedRequest or IOwnerFirmScopedRequest or IWorkspaceTargetRequest;

    private async Task<bool> IsAllowedAsync(TRequest request, DataScope scope, CancellationToken ct)
    {
        if (request is IOwnerFirmScopedRequest owner && !scope.HasFirm(owner.OwnerCompanyId))
            return false;

        if (request is IWorkspaceTargetRequest target && target.WorkspaceId.HasValue && !scope.HasWorkspace(target.WorkspaceId))
            return false;

        if (request is IAuctionScopedRequest auction)
        {
            Guid? firm = await _context.Auctions.Where(a => a.Id == auction.AuctionId).Select(a => (Guid?)a.OwnerCompanyId).FirstOrDefaultAsync(ct);
            if (firm is Guid f && !scope.HasFirm(f)) return false;
        }

        if (request is IRfiScopedRequest rfi)
        {
            Guid? firm = await _context.RfiProcesses.Where(p => p.Id == rfi.RfiProcessId).Select(p => (Guid?)p.OwnerCompanyId).FirstOrDefaultAsync(ct);
            if (firm is Guid f && !scope.HasFirm(f)) return false;
        }

        if (request is IRfqScopedRequest rfq)
        {
            Guid? firm = await _context.RfqProcesses.Where(p => p.Id == rfq.RfqProcessId).Select(p => (Guid?)p.OwnerCompanyId).FirstOrDefaultAsync(ct);
            if (firm is Guid f && !scope.HasFirm(f)) return false;
        }

        if (request is ICompanyScopedRequest company)
        {
            var c = await _context.Companies.Where(x => x.Id == company.CompanyId)
                .Select(x => new
                {
                    x.CompanyType,
                    x.WorkspaceId,
                    Visible = x.VisibleWorkspaces.Select(v => v.WorkspaceId).ToList(),
                }).FirstOrDefaultAsync(ct);
            if (c is not null)
            {
                bool allowed = c.CompanyType == CompanyType.Internal
                    ? scope.HasFirm(company.CompanyId)
                    : scope.HasWorkspace(c.WorkspaceId) || c.Visible.Any(id => scope.HasWorkspace(id));

                // Firmanın ihalesine katılım talebi gönderen (henüz cari onayı almamış) tedarikçi de görüntülenebilir.
                if (!allowed && c.CompanyType != CompanyType.Internal)
                {
                    List<Guid> firmIds = scope.FirmIds.ToList();
                    allowed = await _context.ParticipationRequests.AnyAsync(
                        r => r.CompanyId == company.CompanyId && firmIds.Contains(r.Auction.OwnerCompanyId), ct);
                }

                if (!allowed) return false;
            }
        }

        if (request is IProductScopedRequest product)
        {
            var p = await _context.Products.Where(x => x.Id == product.ProductId).Select(x => new { x.WorkspaceId }).FirstOrDefaultAsync(ct);
            if (p is not null && !scope.HasWorkspace(p.WorkspaceId)) return false;
        }

        return true;
    }
}
