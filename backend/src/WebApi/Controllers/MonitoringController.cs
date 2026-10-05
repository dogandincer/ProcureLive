using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Monitoring.Queries.GetMonitoringStats;
using ProcureLive.Contracts.Monitoring;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/monitoring")]
[Authorize(Policy = Policies.RequireInternalUser)]
public sealed class MonitoringController : ControllerBase
{
    private readonly ISender _sender;

    public MonitoringController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<MonitoringStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetMonitoringStatsQuery(), cancellationToken));
    }
}
