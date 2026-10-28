using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Files;
using ProcureLive.Contracts.Files;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public sealed class FilesController : ControllerBase
{
    private readonly ISender _sender;

    public FilesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Dosya yükleme (multipart, alan adı "file"). purpose: auction-attachment | rfi-answer | product-image | company-document.</summary>
    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ActionResult<UploadedFileDto>> Upload([FromQuery] string purpose, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length > UploadFileCommandHandler.MaxBytes)
        {
            return BadRequest(new { title = "Dosya en fazla 10 MB olabilir." });
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        return Ok(await _sender.Send(new UploadFileCommand(purpose, file.FileName, buffer.ToArray()), cancellationToken));
    }
}
