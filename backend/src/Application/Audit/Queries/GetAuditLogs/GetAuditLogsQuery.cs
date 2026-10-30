using MediatR;
using ProcureLive.Contracts.Audit;
using ProcureLive.Contracts.Common;

namespace ProcureLive.Application.Audit.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    string? EntityName,
    Guid? EntityId,
    int Page,
    int PageSize,
    string? User = null,
    string? Action = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<PagedResult<AuditLogDto>>;

/// <summary>Filtre seçenekleri: kayıtlarda geçen varlık adları ve işlemi yapan kullanıcılar.</summary>
public sealed record GetAuditFacetsQuery : IRequest<AuditFacetsDto>;

public sealed record AuditFacetsDto(IReadOnlyList<string> Entities, IReadOnlyList<string> Users);
