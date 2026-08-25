using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Users.Commands.AssignRoles;
using ProcureLive.Application.Users.Commands.DeactivateUser;
using ProcureLive.Application.Users.Commands.UpdateUser;
using ProcureLive.Application.Users.Queries.GetUsersList;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Users;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.RequireInternalUser)]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetList(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<UserDto> result = await _sender.Send(
            new GetUsersListQuery(searchTerm, companyId, page, pageSize), cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateUserCommand(id, request.FirstName, request.LastName, request.CompanyId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, AssignRolesRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new AssignRolesCommand(id, request.Roles), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeactivateUserCommand(id), cancellationToken);
        return NoContent();
    }
}
