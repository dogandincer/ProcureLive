using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Orders;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Dashboard;

/// <summary>
/// Panodaki "Yapılacaklar": kullanıcının sırada bekleyen işleri. İç kullanıcıda veri kapsamına (işyeri yetkisi) göre,
/// tedarikçide kendi carisine göre sayılır. Tek kayıt varsa <see cref="DashboardTaskDto.TargetId"/> doğrudan o kaydı gösterir.
/// </summary>
public sealed record DashboardTaskDto(string Key, int Count, Guid? TargetId = null, string? TargetKind = null);

public sealed record GetDashboardTasksQuery : IRequest<IReadOnlyList<DashboardTaskDto>>;

public sealed class GetDashboardTasksQueryHandler : IRequestHandler<GetDashboardTasksQuery, IReadOnlyList<DashboardTaskDto>>
{
    private static readonly AuctionStatus[] OpenAuction = [AuctionStatus.Scheduled, AuctionStatus.Live, AuctionStatus.Paused];
    private static readonly ProcessStatus[] OpenProcess = [ProcessStatus.Sent, ProcessStatus.InProgress];

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDataScopeService _scopes;
    private readonly IDateTime _dateTime;

    public GetDashboardTasksQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDataScopeService scopes, IDateTime dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _scopes = scopes;
        _dateTime = dateTime;
    }

    public Task<IReadOnlyList<DashboardTaskDto>> Handle(GetDashboardTasksQuery request, CancellationToken cancellationToken) =>
        _currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Buyer)
            ? InternalAsync(cancellationToken)
            : SupplierAsync(cancellationToken);

    private static async Task Add<T>(List<DashboardTaskDto> tasks, string key, IQueryable<T> source, Func<IQueryable<T>, IQueryable<Guid>> ids,
        string? kind, CancellationToken ct)
    {
        int count = await source.CountAsync(ct);
        if (count == 0) return;
        Guid? target = count == 1 ? await ids(source).FirstOrDefaultAsync(ct) : null;
        tasks.Add(new DashboardTaskDto(key, count, target, kind));
    }

    private async Task<IReadOnlyList<DashboardTaskDto>> InternalAsync(CancellationToken ct)
    {
        var tasks = new List<DashboardTaskDto>();
        DataScope scope = await _scopes.GetAsync(ct);
        IQueryable<Auction> auctions = _context.Auctions.InScope(scope);
        IQueryable<Guid> auctionIds = auctions.Select(a => a.Id);
        IQueryable<Guid> rfiIds = _context.RfiProcesses.InScope(scope).Select(p => p.Id);
        IQueryable<Guid> rfqIds = _context.RfqProcesses.InScope(scope).Select(p => p.Id);
        bool isAdmin = _currentUser.IsInRole(Roles.Admin);

        if (isAdmin)
        {
            await Add(tasks, "publishApprovals", _context.Auctions.Where(a => a.Status == AuctionStatus.Draft && a.ApprovalRequestedAt != null),
                q => q.Select(a => a.Id), "auction", ct);
            await Add(tasks, "pendingSuppliers", _context.Companies.Where(c => c.CompanyType != CompanyType.Internal && c.Status == CompanyStatus.PendingApproval),
                q => q.Select(c => c.Id), "company", ct);
        }

        await Add(tasks, "awaitingAward", auctions.Where(a => a.Status == AuctionStatus.Ended), q => q.Select(a => a.Id), "auction", ct);
        await Add(tasks, "participationRequests",
            _context.ParticipationRequests.Where(r => (r.Status == ParticipationRequestStatus.Pending || r.Status == ParticipationRequestStatus.CompanyApproved)
                && auctionIds.Contains(r.AuctionId)),
            q => q.Select(r => r.AuctionId), "auction", ct);
        var openQuestions = _context.ProcessQuestions.Where(q => !q.IsAnnouncement && q.Answer == null
            && ((q.ProcessKind == ProcessKind.Auction && auctionIds.Contains(q.ProcessId))
                || (q.ProcessKind == ProcessKind.Rfi && rfiIds.Contains(q.ProcessId))
                || (q.ProcessKind == ProcessKind.Rfq && rfqIds.Contains(q.ProcessId))));
        int questions = await openQuestions.CountAsync(ct);
        if (questions > 0)
        {
            // Hepsi aynı süreçteyse doğrudan o sürecin soru-cevap bölümüne gidilir.
            var processes = await openQuestions.Select(q => new { q.ProcessKind, q.ProcessId }).Distinct().Take(2).ToListAsync(ct);
            tasks.Add(processes.Count == 1
                ? new DashboardTaskDto("openQuestions", questions, processes[0].ProcessId, processes[0].ProcessKind.ToString().ToLowerInvariant())
                : new DashboardTaskDto("openQuestions", questions));
        }
        await Add(tasks, "rfqToAward", _context.RfqProcesses.InScope(scope).Where(p => p.Status == ProcessStatus.Closed && p.WinningQuoteId == null),
            q => q.Select(p => p.Id), "rfq", ct);

        IQueryable<PurchaseOrder> orders = _context.PurchaseOrders.AsQueryable();
        if (!scope.IsUnrestricted)
        {
            List<Guid> ws = scope.WorkspaceIds.ToList();
            List<Guid> firms = scope.FirmIds.ToList();
            orders = orders.Where(o => o.WorkspaceId != null ? ws.Contains(o.WorkspaceId.Value) : firms.Contains(o.OwnerCompanyId));
        }
        await Add(tasks, "draftOrders", orders.Where(o => o.Status == PurchaseOrderStatus.Draft), q => q.Select(o => o.Id), "order", ct);

        // Cari belgeleri: Admin tümünü, satınalmacı birincil işyeri kapsamındaki carilerin belgelerini görür.
        DateTime limit = _dateTime.UtcNow.Date.AddDays(30);
        var docs = _context.CompanyDocuments.Where(d => d.ValidUntil != null && d.ValidUntil <= limit);
        if (!scope.IsUnrestricted)
        {
            List<Guid> ws = scope.WorkspaceIds.ToList();
            docs = docs.Where(d => d.Company.WorkspaceId != null && ws.Contains(d.Company.WorkspaceId.Value));
        }
        await Add(tasks, "expiringDocuments", docs, q => q.Select(d => d.CompanyId), "company", ct);

        return tasks;
    }

    private async Task<IReadOnlyList<DashboardTaskDto>> SupplierAsync(CancellationToken ct)
    {
        var tasks = new List<DashboardTaskDto>();
        if (_currentUser.CompanyId is not Guid companyId) return tasks;

        await Add(tasks, "auctionInvitations",
            _context.AuctionInvitations.Where(i => i.CompanyId == companyId && i.Status == InvitationStatus.Invited && OpenAuction.Contains(i.Auction.Status)),
            q => q.Select(i => i.AuctionId), "auction", ct);
        await Add(tasks, "rfiInvitations",
            _context.RfiInvitations.Where(i => i.CompanyId == companyId && i.Status == InvitationStatus.Invited && OpenProcess.Contains(i.RfiProcess.Status)),
            q => q.Select(i => i.RfiProcessId), "rfi", ct);
        await Add(tasks, "rfqInvitations",
            _context.RfqInvitations.Where(i => i.CompanyId == companyId && i.Status == InvitationStatus.Invited && OpenProcess.Contains(i.RfqProcess.Status)),
            q => q.Select(i => i.RfqProcessId), "rfq", ct);
        await Add(tasks, "ordersToConfirm",
            _context.PurchaseOrders.Where(o => o.SupplierCompanyId == companyId && o.Status == PurchaseOrderStatus.Sent),
            q => q.Select(o => o.Id), "order", ct);
        await Add(tasks, "pendingParticipation",
            _context.ParticipationRequests.Where(r => r.CompanyId == companyId
                && (r.Status == ParticipationRequestStatus.Pending || r.Status == ParticipationRequestStatus.CompanyApproved)),
            q => q.Select(r => r.AuctionId), "auction", ct);
        DateTime limit = _dateTime.UtcNow.Date.AddDays(30);
        await Add(tasks, "myExpiringDocuments",
            _context.CompanyDocuments.Where(d => d.CompanyId == companyId && d.ValidUntil != null && d.ValidUntil <= limit),
            q => q.Select(d => d.CompanyId), "company", ct);
        return tasks;
    }
}
