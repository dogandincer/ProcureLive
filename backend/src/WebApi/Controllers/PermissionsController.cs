using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.WebApi.Controllers;

/// <summary>İşlem bazlı yetkiler: Satınalmacı rolünün yetkileri (Admin) ve oturumdaki kullanıcının yetkileri (arayüz için).</summary>
[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class PermissionsController : ControllerBase
{
    private readonly ISender _sender;

    public PermissionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> Mine(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetMyPermissionsQuery(), cancellationToken));
    }

    [HttpGet("buyer")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<IReadOnlyList<RolePermissionDto>>> GetBuyer(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetBuyerPermissionsQuery(), cancellationToken));
    }

    [HttpPut("buyer")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> SetBuyer(SetBuyerPermissionsRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetBuyerPermissionsCommand(request.Codes), cancellationToken);
        return NoContent();
    }
}

public sealed record SetBuyerPermissionsRequest(IReadOnlyCollection<string> Codes);
