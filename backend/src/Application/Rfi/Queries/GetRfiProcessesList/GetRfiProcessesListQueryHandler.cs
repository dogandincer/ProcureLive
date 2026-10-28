using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Queries.GetRfiProcessesList;

public sealed class GetRfiProcessesListQueryHandler : IRequestHandler<GetRfiProcessesListQuery, PagedResult<RfiProcessDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDataScopeService _scope;
    private readonly ISupplierAccessService _accessService;

    public GetRfiProcessesListQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService, IDataScopeService scope, ISupplierAccessService accessService)
    {
        _scope = scope;
        _context = context;
        _currentUserService = currentUserService;
        _accessService = accessService;
    }

    public async Task<PagedResult<RfiProcessDto>> Handle(GetRfiProcessesListQuery request, CancellationToken cancellationToken)
    {
        IQueryable<RfiProcess> query = _context.RfiProcesses
            .Include(p => p.OwnerCompany)
            .Include(p => p.RfiTemplate)
            .Include(p => p.Invitations)
            .Include(p => p.Responses)
            .AsQueryable();

        bool isInternalUser = _currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.Buyer);
        Guid supplierCompanyId = _currentUserService.CompanyId ?? Guid.Empty;
        SupplierAccessContext? access = null;

        if (!isInternalUser)
        {
            query = query.Where(p => p.Invitations.Any(i => i.CompanyId == supplierCompanyId));
            access = await _accessService.GetContextAsync(supplierCompanyId, cancellationToken);
        }
        else
        {
            // Satınalmacı yalnızca yetkili işyerlerinin süreçlerini görür (işyeri atanmamış eski süreçte firma yetkisi).
            query = query.InScope(await _scope.GetAsync(cancellationToken));

            // Başlıktaki seçim: işyeri seçiliyse o işyeri, "Tüm İşyerleri"nde firmanın tümü.
            if (request.WorkspaceId is Guid workspaceId)
                query = query.Where(p => p.WorkspaceId == workspaceId);
            else if (request.FirmId is Guid firmId)
                query = query.Where(p => p.OwnerCompanyId == firmId);
        }

        // Başlık veya referans kodunda arama (büyük/küçük harf duyarsız).
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Title.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            ProcessStatus status;
            bool parsed = Enum.TryParse(request.Status, true, out status);

            if (parsed)
            {
                query = query.Where(p => p.Status == status);
            }
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        List<RfiProcess> processes = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> workspaceNames = await WorkspaceNamesAsync(processes.Select(p => p.WorkspaceId), access is null, cancellationToken);

        List<RfiProcessDto> items = processes.Select(p =>
        {
            if (access is null)
            {
                return new RfiProcessDto(
                    p.Id, p.RfiTemplateId, p.RfiTemplate.Name, p.Title, p.OwnerCompanyId, p.OwnerCompany.Name,
                    p.Status.ToString(), p.DueDate, p.Invitations.Count,
                    p.Responses.Select(r => r.CompanyId).Distinct().Count(), p.CreatedAt,
                    WorkspaceName: p.WorkspaceId is Guid w ? workspaceNames.GetValueOrDefault(w) : null);
            }

            // Tedarikçi: kendi davet durumu + kota kısıtı; rakip davet/yanıt sayıları dönmez.
            InvitationStatus? myStatus = p.Invitations.FirstOrDefault(i => i.CompanyId == supplierCompanyId)?.Status;
            AuctionAccessLevel level = ProcessParticipationRules.Evaluate(access, myStatus);
            return new RfiProcessDto(
                p.Id, p.RfiTemplateId, p.RfiTemplate.Name, p.Title, p.OwnerCompanyId, p.OwnerCompany.Name,
                p.Status.ToString(), p.DueDate, 0, 0, p.CreatedAt, level.ToString(), myStatus?.ToString());
        }).ToList();

        return new PagedResult<RfiProcessDto>(items, totalCount, page, pageSize);
    }

    private async Task<Dictionary<Guid, string>> WorkspaceNamesAsync(IEnumerable<Guid?> ids, bool internalUser, CancellationToken ct)
    {
        if (!internalUser) return new Dictionary<Guid, string>();
        List<Guid> wanted = ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        return await _context.Workspaces.Where(w => wanted.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.Name, ct);
    }
}
