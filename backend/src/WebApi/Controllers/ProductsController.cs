using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Catalog.Products.Commands.AddProductImage;
using ProcureLive.Application.Catalog.Products.Commands.AssignProductWorkspace;
using ProcureLive.Application.Catalog.Products.Commands.CreateProduct;
using ProcureLive.Application.Catalog.Products.Commands.DeleteProduct;
using ProcureLive.Application.Catalog.Products.Commands.GenerateProduct3dModel;
using ProcureLive.Application.Catalog.Products.Commands.RemoveProduct3dModel;
using ProcureLive.Application.Catalog.Products.Commands.RemoveProductImage;
using ProcureLive.Application.Catalog.Products.Commands.SetProduct3dModel;
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
        [FromQuery] Guid? firmId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<ProductDto> result = await _sender.Send(
            new GetProductsListQuery(searchTerm, categoryId, isActive, workspaceId, page, pageSize, firmId), cancellationToken);

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

    /// <summary>Set an interactive 3D model for the product by direct .glb/.gltf URL.</summary>
    [HttpPut("{id:guid}/model-3d")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Set3dModel(Guid id, SetProduct3dModelRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetProduct3dModelCommand(id, request.Url), cancellationToken);
        return NoContent();
    }

    /// <summary>Generate a 3D model from the product's photos (simulated). Returns the model URL.</summary>
    [HttpPost("{id:guid}/model-3d/generate")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<string>> Generate3dModel(Guid id, CancellationToken cancellationToken)
    {
        string url = await _sender.Send(new GenerateProduct3dModelCommand(id), cancellationToken);
        return Ok(new { url });
    }

    [HttpDelete("{id:guid}/model-3d")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Remove3dModel(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveProduct3dModelCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Assign (or clear, with null) the product's workspace.</summary>
    [HttpPut("{id:guid}/workspace")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> AssignWorkspace(Guid id, AssignProductWorkspaceRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new AssignProductWorkspaceCommand(id, request.WorkspaceId), cancellationToken);
        return NoContent();
    }
}
