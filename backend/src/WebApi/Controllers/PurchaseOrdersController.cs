using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Orders;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Orders;

namespace ProcureLive.WebApi.Controllers;

/// <summary>Siparişler: iç kullanıcı kapsamındakileri, tedarikçi kendisine gönderilenleri görür.</summary>
[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public sealed class PurchaseOrdersController : ControllerBase
{
    private readonly ISender _sender;

    public PurchaseOrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderListItemDto>>> GetList(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] string? sourceKind,
        [FromQuery] Guid? sourceId,
        [FromQuery] Guid? firmId,
        [FromQuery] Guid? workspaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(
            new GetPurchaseOrdersQuery(status, search, sourceKind, sourceId, firmId, workspaceId, page, pageSize), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetPurchaseOrderQuery(id), cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Update(Guid id, UpdatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdatePurchaseOrderCommand(id, request.Notes, request.DeliveryDate), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new SendPurchaseOrderCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Cancel(Guid id, CancelPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new CancelPurchaseOrderCommand(id, request.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<IActionResult> Confirm(Guid id, ConfirmPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new ConfirmPurchaseOrderCommand(id, request.Note), cancellationToken);
        return NoContent();
    }
}
