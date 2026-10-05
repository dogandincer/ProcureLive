using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Catalog.Products.Commands.AddProductImage;
using ProcureLive.Application.Catalog.Products.Commands.CreateProduct;
using ProcureLive.Application.Catalog.Products.Commands.DeleteProduct;
using ProcureLive.Application.Catalog.Products.Commands.RemoveProductImage;
using ProcureLive.Application.Catalog.Products.Commands.UpdateProduct;
using ProcureLive.Application.Catalog.Products.Queries.GetProductById;
using ProcureLive.Application.Catalog.Products.Queries.GetProductsList;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Catalog;
using ProcureLive.Contracts.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetList(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] Guid? workspaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<ProductDto> result = await _sender.Send(
            new GetProductsListQuery(searchTerm, categoryId, isActive, workspaceId, page, pageSize), cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        ProductDto result = await _sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        UnitOfMeasure unitOfMeasure;
        bool parsed = Enum.TryParse(request.UnitOfMeasure, true, out unitOfMeasure);

        if (!parsed)
        {
            return BadRequest($"Geçersiz UnitOfMeasure: {request.UnitOfMeasure}");
        }

        Guid id = await _sender.Send(
            new CreateProductCommand(request.Sku, request.Name, request.Description, request.CategoryId, unitOfMeasure, request.Brand, request.DefaultCurrencyId, request.WorkspaceId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        UnitOfMeasure unitOfMeasure;
        bool parsed = Enum.TryParse(request.UnitOfMeasure, true, out unitOfMeasure);

        if (!parsed)
        {
            return BadRequest($"Geçersiz UnitOfMeasure: {request.UnitOfMeasure}");
        }

        await _sender.Send(
            new UpdateProductCommand(id, request.Name, request.Description, request.CategoryId, unitOfMeasure, request.Brand, request.DefaultCurrencyId, request.IsActive),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/images")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> AddImage(Guid id, AddProductImageRequest request, CancellationToken cancellationToken)
    {
        Guid imageId = await _sender.Send(
            new AddProductImageCommand(id, request.Url, request.IsPrimary, request.SortOrder), cancellationToken);

        return Ok(imageId);
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> RemoveImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveProductImageCommand(id, imageId), cancellationToken);
        return NoContent();
    }
}
