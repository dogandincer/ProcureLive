using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Notifications.Email;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Notifications;

namespace ProcureLive.WebApi.Controllers;

/// <summary>Giden e-posta (SMTP) ayarı, test gönderimi ve bildirim e-postası kayıtları — yalnızca Admin.</summary>
[ApiController]
[Route("api/settings/email")]
[Authorize(Policy = Policies.RequireAdmin)]
public sealed class EmailSettingsController : ControllerBase
{
    private readonly ISender _sender;

    public EmailSettingsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<EmailSettingsDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetEmailSettingsQuery(), cancellationToken));
    }

    [HttpPut]
    public async Task<IActionResult> Update(EmailSettingsRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateEmailSettingsCommand(request), cancellationToken);
        return NoContent();
    }

    /// <summary>Formdaki ayarla test iletisi gönderir; SMTP hatası 200 + Succeeded=false olarak döner.</summary>
    [HttpPost("test")]
    public async Task<ActionResult<TestEmailResultDto>> Test(SendTestEmailRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new SendTestEmailCommand(request.To, request.Settings), cancellationToken));
    }

    [HttpGet("logs")]
    public async Task<ActionResult<PagedResult<EmailLogDto>>> Logs(
        [FromQuery] string? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new GetEmailLogsQuery(status, search, page, pageSize), cancellationToken));
    }

    [HttpGet("logs/summary")]
    public async Task<ActionResult<EmailLogSummaryDto>> Summary(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetEmailLogSummaryQuery(), cancellationToken));
    }

    [HttpPost("logs/{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new RetryEmailCommand(id), cancellationToken);
        return NoContent();
    }
}
