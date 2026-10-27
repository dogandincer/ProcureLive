using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Firms.Commands;
using ProcureLive.Application.Firms.Queries;
using ProcureLive.Contracts.Firms;

namespace ProcureLive.WebApi.Controllers;

/// <summary>
/// Firma tanımı (işyerlerinin üst ebeveyni). Liste iç kullanıcılara açık (başlıktaki firma seçimi);
/// oluşturma/değiştirme/silme/logo yalnızca Admin.
/// </summary>
[ApiController]
[Route("api/firms")]
[Authorize]
public sealed class FirmsController : ControllerBase
{
    private readonly ISender _sender;

    public FirmsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<List<FirmDto>>> GetList(CancellationToken cancellationToken)
    {
        List<FirmDto> result = await _sender.Send(new GetFirmsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<ActionResult<Guid>> Create(SaveFirmRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(new SaveFirmCommand(null, request), cancellationToken);
        return Ok(id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Update(Guid id, SaveFirmRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SaveFirmCommand(id, request), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/default")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetDefaultFirmCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteFirmCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Logo yükleme (multipart, alan adı "file"). İçerik imzası sunucuda doğrulanır.</summary>
    [HttpPost("{id:guid}/logo")]
    [Authorize(Policy = Policies.RequireAdmin)]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length > UploadFirmLogoCommandHandler.MaxBytes)
        {
            return BadRequest("Logo dosyası en fazla 2 MB olabilir.");
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        string url = await _sender.Send(new UploadFirmLogoCommand(id, buffer.ToArray()), cancellationToken);
        return Ok(new { logoUrl = url });
    }

    [HttpDelete("{id:guid}/logo")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> RemoveLogo(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveFirmLogoCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Giriş ekranı/sekme markası — oturum açılmadan okunur.</summary>
[ApiController]
[Route("api/branding")]
public sealed class BrandingController : ControllerBase
{
    private readonly ISender _sender;

    public BrandingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<BrandingDto>> Get(CancellationToken cancellationToken)
    {
        BrandingDto result = await _sender.Send(new GetBrandingQuery(), cancellationToken);
        return Ok(result);
    }
}
