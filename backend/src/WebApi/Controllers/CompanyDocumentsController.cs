using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Companies.Documents;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Controllers;

/// <summary>Cari belgeleri: iç kullanıcı (kapsamındaki cari) ve tedarikçi (yalnızca kendi firması).</summary>
[ApiController]
[Route("api/companies/{companyId:guid}/documents")]
[Authorize]
public sealed class CompanyDocumentsController : ControllerBase
{
    private readonly ISender _sender;

    public CompanyDocumentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompanyDocumentDto>>> GetList(Guid companyId, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetCompanyDocumentsQuery(companyId), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Add(Guid companyId, AddCompanyDocumentRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.DocumentType, true, out CompanyDocumentType type) || !Enum.IsDefined(type))
        {
            return BadRequest($"Geçersiz belge türü: {request.DocumentType}");
        }

        return Ok(await _sender.Send(
            new AddCompanyDocumentCommand(companyId, type, request.Title, request.FileUrl, request.FileName, request.ValidUntil), cancellationToken));
    }

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Delete(Guid companyId, Guid documentId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCompanyDocumentCommand(companyId, documentId), cancellationToken);
        return NoContent();
    }
}
