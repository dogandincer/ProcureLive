using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Dashboard;

namespace ProcureLive.WebApi.Controllers;

/// <summary>Panodaki "Yapılacaklar" kartı: role ve veri kapsamına göre bekleyen işlerin sayıları.</summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<IReadOnlyList<DashboardTaskDto>>> Tasks(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetDashboardTasksQuery(), cancellationToken));
    }
}
