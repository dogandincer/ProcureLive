using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Audit.Queries.GetAuditLogs;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Audit;
using ProcureLive.Contracts.Common;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = Policies.RequireAdmin)]
public sealed class AuditController : ControllerBase
{
    private readonly ISender _sender;

    public AuditController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetList(
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] string? user,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new GetAuditLogsQuery(entityName, entityId, page, pageSize, user, action, from, to), cancellationToken));
    }

    /// <summary>Filtre seçenekleri (varlık adları, kullanıcılar).</summary>
    [HttpGet("facets")]
    public async Task<ActionResult<AuditFacetsDto>> Facets(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetAuditFacetsQuery(), cancellationToken));
    }
}
