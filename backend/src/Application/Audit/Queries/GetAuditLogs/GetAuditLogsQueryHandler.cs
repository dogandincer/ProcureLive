using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Audit;
using ProcureLive.Contracts.Common;
using ProcureLive.Domain.Entities.Audit;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Audit.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        // RefreshToken churns on every login and carries no business meaning - excluded to keep the log readable.
        IQueryable<AuditLog> query = _context.AuditLogs.Where(a => a.EntityName != "RefreshToken");

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            query = query.Where(a => a.EntityName == request.EntityName);
        }

        if (request.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == request.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.User))
        {
            string user = request.User.Trim().ToLowerInvariant();
            query = query.Where(a => a.ChangedByUserName != null && a.ChangedByUserName.ToLower().Contains(user));
        }

        if (Enum.TryParse(request.Action, true, out AuditAction action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (request.From is DateTime from)
        {
            DateTime start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
            query = query.Where(a => a.ChangedAt >= start);
        }

        if (request.To is DateTime to)
        {
            DateTime end = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(a => a.ChangedAt < end);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page < 1 ? 1 : request.Page;
        int pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        List<AuditLogDto> items = await query
            .OrderByDescending(a => a.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.EntityName, a.EntityId, a.Action.ToString(), a.ChangedByUserId, a.ChangedByUserName,
                a.ChangedAt, a.OldValues, a.NewValues, a.AffectedColumns))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, totalCount, page, pageSize);
    }
}

public sealed class GetAuditFacetsQueryHandler : IRequestHandler<GetAuditFacetsQuery, AuditFacetsDto>
{
    private readonly IApplicationDbContext _context;

    public GetAuditFacetsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditFacetsDto> Handle(GetAuditFacetsQuery request, CancellationToken cancellationToken)
    {
        List<string> entities = await _context.AuditLogs.Where(a => a.EntityName != "RefreshToken")
            .Select(a => a.EntityName).Distinct().OrderBy(n => n).ToListAsync(cancellationToken);
        List<string> users = await _context.AuditLogs.Where(a => a.ChangedByUserName != null)
            .Select(a => a.ChangedByUserName!).Distinct().OrderBy(n => n).ToListAsync(cancellationToken);
        return new AuditFacetsDto(entities, users);
    }
}
