using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Sectors.Commands.CreateSector;
using ProcureLive.Application.Sectors.Commands.DeleteSector;
using ProcureLive.Application.Sectors.Commands.UpdateSector;
using ProcureLive.Application.Sectors.Queries.GetSectorsList;
using ProcureLive.Contracts.Companies;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/sectors")]
[Authorize]
public sealed class SectorsController : ControllerBase
{
    private readonly ISender _sender;

    public SectorsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<SectorDto>>> GetList(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken = default)
    {
        List<SectorDto> result = await _sender.Send(new GetSectorsListQuery(activeOnly), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Create(CreateSectorRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(new CreateSectorCommand(request.Name, request.Description), cancellationToken);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Update(Guid id, UpdateSectorRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateSectorCommand(id, request.Name, request.Description, request.IsActive), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteSectorCommand(id), cancellationToken);
        return NoContent();
    }
}
