using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Workspaces.Commands.CreateWorkspace;
using ProcureLive.Application.Workspaces.Commands.DeleteWorkspace;
using ProcureLive.Application.Workspaces.Commands.UpdateWorkspace;
using ProcureLive.Application.Workspaces.Queries.GetWorkspacesList;
using ProcureLive.Contracts.Workspaces;

namespace ProcureLive.WebApi.Controllers;

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
        CancellationToken cancellationToken = default)
    {
        List<WorkspaceDto> result = await _sender.Send(new GetWorkspacesListQuery(activeOnly), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Create(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(new CreateWorkspaceCommand(request.Name, request.Description), cancellationToken);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Update(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateWorkspaceCommand(id, request.Name, request.Description, request.IsActive), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteWorkspaceCommand(id), cancellationToken);
        return NoContent();
    }
}
