using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Workspaces.Commands.CreateWorkspace;
using ProcureLive.Application.Workspaces.Commands.DeleteWorkspace;
using ProcureLive.Application.Workspaces.Commands.SetDefaultWorkspace;
using ProcureLive.Application.Workspaces.Commands.UpdateWorkspace;
using ProcureLive.Application.Workspaces.Queries.GetWorkspacesList;
using ProcureLive.Contracts.Workspaces;

namespace ProcureLive.WebApi.Controllers;

/// <summary>İşyeri tanımı yalnızca Admin'dir; liste tüm oturumlara açıktır (başlıktaki işyeri seçimi).</summary>
[ApiController]
[Route("api/workspaces")]
[Authorize]
public sealed class WorkspacesController : ControllerBase
{
    private readonly ISender _sender;

    public WorkspacesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkspaceDto>>> GetList(
        [FromQuery] bool? activeOnly,
        [FromQuery] Guid? firmId,
        CancellationToken cancellationToken = default)
    {
        List<WorkspaceDto> result = await _sender.Send(new GetWorkspacesListQuery(activeOnly, firmId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<Guid>> Create(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(
            new CreateWorkspaceCommand(request.Name, request.Description, request.FirmId, request.IsDefault), cancellationToken);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Update(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(
            new UpdateWorkspaceCommand(id, request.Name, request.Description, request.IsActive, request.FirmId, request.IsDefault),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/default")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetDefaultWorkspaceCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteWorkspaceCommand(id), cancellationToken);
        return NoContent();
    }
}
