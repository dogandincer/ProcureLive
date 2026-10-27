using MediatR;
using ProcureLive.Contracts.Workspaces;
using ProcureLive.Application.Workspaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Rfi.Commands.AddRfiQuestion;
using ProcureLive.Application.Rfi.Commands.CloseRfiProcess;
using ProcureLive.Application.Rfi.Commands.CreateRfiProcess;
using ProcureLive.Application.Rfi.Commands.CreateRfiTemplate;
using ProcureLive.Application.Rfi.Commands.EvaluateSupplier;
using ProcureLive.Application.Rfi.Commands.InviteCompaniesToRfi;
using ProcureLive.Application.Rfi.Commands.RespondToRfiInvitation;
using ProcureLive.Application.Rfi.Commands.SubmitRfiResponses;
using ProcureLive.Application.Rfi.Queries.GetRfiProcessById;
using ProcureLive.Application.Rfi.Queries.GetRfiProcessesList;
using ProcureLive.Application.Rfi.Queries.GetRfiResponses;
using ProcureLive.Application.Rfi.Queries.GetRfiTemplateById;
using ProcureLive.Application.Rfi.Queries.GetRfiTemplatesList;
using ProcureLive.Application.Rfi.Queries.GetSupplierEvaluations;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/rfi/templates")]
[Authorize(Policy = Policies.RequireInternalUser)]
public sealed class RfiTemplatesController : ControllerBase
{
    private readonly ISender _sender;

    public RfiTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RfiTemplateDto>>> GetList(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetRfiTemplatesListQuery(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RfiTemplateDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetRfiTemplateByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateRfiTemplateRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(new CreateRfiTemplateCommand(request.Name, request.Description, request.CategoryId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/questions")]
    public async Task<ActionResult<Guid>> AddQuestion(Guid id, AddRfiQuestionRequest request, CancellationToken cancellationToken)
    {
        RfiQuestionType questionType;
        bool parsed = Enum.TryParse(request.QuestionType, true, out questionType);

        if (!parsed)
        {
            return BadRequest($"Geçersiz QuestionType: {request.QuestionType}");
        }

        Guid questionId = await _sender.Send(
            new AddRfiQuestionCommand(id, request.QuestionText, questionType, request.IsRequired, request.SortOrder, request.Weight, request.Options),
            cancellationToken);

        return Ok(questionId);
    }
}

[ApiController]
[Route("api/rfi/processes")]
[Authorize]
public sealed class RfiProcessesController : ControllerBase
{
    private readonly ISender _sender;

    public RfiProcessesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RfiProcessDto>>> GetList(
        [FromQuery] string? status,
        [FromQuery] Guid? firmId,
        [FromQuery] Guid? workspaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new GetRfiProcessesListQuery(status, page, pageSize, firmId, workspaceId), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RfiProcessDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetRfiProcessByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Create(CreateRfiProcessRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(
            new CreateRfiProcessCommand(request.RfiTemplateId, request.Title, request.OwnerCompanyId, request.DueDate, request.WorkspaceId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Sürecin işyeri (satınalmacı kapsamı bu işyerine göre). Kapanmış süreçte değişmez.</summary>
    [HttpPut("{id:guid}/workspace")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> ChangeWorkspace(Guid id, ChangeProcessWorkspaceRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new ChangeRfiWorkspaceCommand(id, request.WorkspaceId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/invitations")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> InviteCompanies(Guid id, InviteCompaniesToRfiRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new InviteCompaniesToRfiCommand(id, request.CompanyIds), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/invitations/respond")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<IActionResult> RespondToInvitation(Guid id, RespondToRfiInvitationRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new RespondToRfiInvitationCommand(id, request.Accept, request.DeclineReason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/responses")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<IActionResult> SubmitResponses(Guid id, SubmitRfiResponsesRequest request, CancellationToken cancellationToken)
    {
        var answers = request.Answers.Select(a => new Application.Rfi.Commands.SubmitRfiResponses.RfiAnswerInput(
            a.RfiQuestionId, a.AnswerText, a.AnswerNumber, a.AnswerBool, a.SelectedOptionId, a.AttachmentUrl)).ToList();

        await _sender.Send(new SubmitRfiResponsesCommand(id, answers), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/responses")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<IReadOnlyCollection<RfiResponseDto>>> GetResponses(
        Guid id, [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetRfiResponsesQuery(id, companyId), cancellationToken));
    }

    [HttpPost("{id:guid}/evaluations")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> EvaluateSupplier(Guid id, EvaluateSupplierRequest request, CancellationToken cancellationToken)
    {
        Guid evaluationId = await _sender.Send(
            new EvaluateSupplierCommand(id, request.CompanyId, request.TotalScore, request.Comments), cancellationToken);

        return Ok(evaluationId);
    }

    [HttpGet("{id:guid}/evaluations")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<IReadOnlyCollection<SupplierEvaluationDto>>> GetEvaluations(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetSupplierEvaluationsQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new CloseRfiProcessCommand(id), cancellationToken);
        return NoContent();
    }
}
