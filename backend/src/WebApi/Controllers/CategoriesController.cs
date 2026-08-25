using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Catalog.Categories.Commands.CreateCategory;
using ProcureLive.Application.Catalog.Categories.Commands.DeleteCategory;
using ProcureLive.Application.Catalog.Categories.Commands.UpdateCategory;
using ProcureLive.Application.Catalog.Categories.Queries.GetCategoriesTree;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Catalog;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public sealed class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CategoryDto>>> GetTree(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CategoryDto> result = await _sender.Send(new GetCategoriesTreeQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(new CreateCategoryCommand(request.Name, request.Code, request.ParentCategoryId), cancellationToken);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateCategoryCommand(id, request.Name, request.Code, request.ParentCategoryId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
