using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Collaboration;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Collaboration;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Controllers;

/// <summary>Süreç soru-cevabı ve duyurular. kind: auction | rfi | rfq.</summary>
[ApiController]
[Authorize]
public sealed class ProcessQuestionsController : ControllerBase
{
    private readonly ISender _sender;

    public ProcessQuestionsController(ISender sender)
    {
        _sender = sender;
    }

    private static bool TryKind(string kind, out ProcessKind result) =>
        Enum.TryParse(kind, true, out result) && Enum.IsDefined(result);

    [HttpGet("api/processes/{kind}/{processId:guid}/questions")]
    public async Task<ActionResult<IReadOnlyList<ProcessQuestionDto>>> GetList(string kind, Guid processId, CancellationToken cancellationToken)
    {
        if (!TryKind(kind, out ProcessKind k)) return BadRequest($"Geçersiz süreç türü: {kind}");
        return Ok(await _sender.Send(new GetProcessQuestionsQuery(k, processId), cancellationToken));
    }

    [HttpPost("api/processes/{kind}/{processId:guid}/questions")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<ActionResult<Guid>> Ask(string kind, Guid processId, AskQuestionRequest request, CancellationToken cancellationToken)
    {
        if (!TryKind(kind, out ProcessKind k)) return BadRequest($"Geçersiz süreç türü: {kind}");
        return Ok(await _sender.Send(new AskProcessQuestionCommand(k, processId, request.Question), cancellationToken));
    }

    [HttpPost("api/processes/{kind}/{processId:guid}/announcements")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Announce(string kind, Guid processId, AnnouncementRequest request, CancellationToken cancellationToken)
    {
        if (!TryKind(kind, out ProcessKind k)) return BadRequest($"Geçersiz süreç türü: {kind}");
        return Ok(await _sender.Send(new PostProcessAnnouncementCommand(k, processId, request.Text), cancellationToken));
    }

    [HttpPut("api/process-questions/{id:guid}/answer")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Answer(Guid id, AnswerQuestionRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new AnswerProcessQuestionCommand(id, request.Answer, request.IsPublic), cancellationToken);
        return NoContent();
    }
}
