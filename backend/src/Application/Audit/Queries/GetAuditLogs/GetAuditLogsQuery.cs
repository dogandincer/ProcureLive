using MediatR;
using ProcureLive.Contracts.Audit;
using ProcureLive.Contracts.Common;

namespace ProcureLive.Application.Audit.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    string? EntityName,
    Guid? EntityId,
    int Page,
    int PageSize) : IRequest<PagedResult<AuditLogDto>>;
