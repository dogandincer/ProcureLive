using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Erp;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Erp;

namespace ProcureLive.WebApi.Controllers;

/// <summary>
/// Premium ERP (UyumERP) entegrasyonu. Her uç önce Features:ErpIntegration anahtarını denetler (kapalıysa 403 PremiumRequired).
/// Ayar, aktarım ve kayıtlar yalnızca Admin; kartlardaki durum iç kullanıcılara; ERP siparişi orders.manage yetkisiyle.
/// </summary>
[ApiController]
[Authorize]
public sealed class ErpController : ControllerBase
{
    private readonly ISender _sender;

    public ErpController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Premium özellik durumu (tüm oturumlar): ön yüz menüyü ve düğmeleri buna göre gösterir.</summary>
    [HttpGet("api/features")]
    public async Task<ActionResult<FeaturesDto>> Features(CancellationToken ct) => Ok(await _sender.Send(new GetFeaturesQuery(), ct));

    [HttpGet("api/erp/settings")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<ErpSettingsDto>> GetSettings(CancellationToken ct) => Ok(await _sender.Send(new GetErpSettingsQuery(), ct));

    [HttpPut("api/erp/settings")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UpdateSettings(ErpSettingsRequest request, CancellationToken ct)
    {
        await _sender.Send(new UpdateErpSettingsCommand(request), ct);
        return NoContent();
    }

    /// <summary>Formdaki bağlantıyla salt okuma testi; hata 200 + Succeeded=false.</summary>
    [HttpPost("api/erp/settings/test")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<ErpTestResultDto>> Test(ErpSettingsRequest request, CancellationToken ct) =>
        Ok(await _sender.Send(new TestErpConnectionCommand(request), ct));

    [HttpGet("api/erp/entities")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<IReadOnlyList<ErpEntityDto>>> SearchEntities([FromQuery] string? search, CancellationToken ct) =>
        Ok(await _sender.Send(new SearchErpEntitiesQuery(search), ct));

    [HttpPost("api/erp/entities/import")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<ErpImportResultDto>> ImportEntities(ErpImportRequest request, CancellationToken ct) =>
        Ok(await _sender.Send(new ImportErpEntitiesCommand(request), ct));

    [HttpGet("api/erp/items")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<IReadOnlyList<ErpItemDto>>> SearchItems([FromQuery] string? search, CancellationToken ct) =>
        Ok(await _sender.Send(new SearchErpItemsQuery(search), ct));

    [HttpPost("api/erp/items/import")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<ErpImportResultDto>> ImportItems(ErpImportRequest request, CancellationToken ct) =>
        Ok(await _sender.Send(new ImportErpItemsCommand(request), ct));

    [HttpPost("api/erp/companies/{id:guid}/export")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> ExportCompany(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ExportCompanyToErpCommand(id), ct);
        return Accepted();
    }

    [HttpPost("api/erp/products/{id:guid}/export")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> ExportProduct(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ExportProductToErpCommand(id), ct);
        return Accepted();
    }

    [HttpPost("api/erp/purchase-orders/{id:guid}/order")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> CreateOrder(Guid id, CancellationToken ct)
    {
        await _sender.Send(new CreateErpOrderCommand(id), ct);
        return Accepted();
    }

    [HttpGet("api/erp/status")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<ErpRecordStatusDto>> Status(
        [FromQuery] Guid? companyId, [FromQuery] Guid? productId, [FromQuery] Guid? purchaseOrderId, [FromQuery] Guid? auctionId, CancellationToken ct) =>
        Ok(await _sender.Send(new GetErpRecordStatusQuery(companyId, productId, purchaseOrderId, auctionId), ct));

    [HttpGet("api/erp/jobs")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<PagedResult<ErpSyncJobDto>>> Jobs(
        [FromQuery] string? status, [FromQuery] string? kind, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await _sender.Send(new GetErpJobsQuery(status, kind, search, page, pageSize), ct));

    [HttpGet("api/erp/jobs/summary")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<ErpSyncSummaryDto>> JobSummary(CancellationToken ct) => Ok(await _sender.Send(new GetErpJobSummaryQuery(), ct));

    [HttpGet("api/erp/jobs/{id:guid}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<ErpSyncJobDetailDto>> Job(Guid id, CancellationToken ct) => Ok(await _sender.Send(new GetErpJobQuery(id), ct));

    [HttpPost("api/erp/jobs/{id:guid}/retry")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        await _sender.Send(new RetryErpJobCommand(id), ct);
        return NoContent();
    }
}
