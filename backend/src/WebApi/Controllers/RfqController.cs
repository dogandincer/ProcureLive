using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Rfq.Commands.AddRfqLineItem;
using ProcureLive.Application.Rfq.Commands.AwardRfqProcess;
using ProcureLive.Application.Rfq.Commands.CloseRfqProcess;
using ProcureLive.Application.Rfq.Commands.CreateRfqProcess;
using ProcureLive.Application.Rfq.Commands.InviteCompaniesToRfq;
using ProcureLive.Application.Rfq.Commands.PublishRfqProcess;
using ProcureLive.Application.Rfq.Commands.RespondToRfqInvitation;
using ProcureLive.Application.Rfq.Commands.SubmitRfqQuote;
using ProcureLive.Application.Rfq.Queries.GetRfqAiRecommendation;
using ProcureLive.Application.Rfq.Queries.GetRfqComparisonMatrix;
using ProcureLive.Application.Rfq.Queries.GetRfqProcessById;
using ProcureLive.Application.Rfq.Queries.GetRfqProcessesList;
using ProcureLive.Application.Rfq.Queries.GetRfqQuotes;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/rfq/processes")]
[Authorize]
public sealed class RfqController : ControllerBase
{
    private readonly ISender _sender;

    public RfqController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RfqProcessDto>>> GetList(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new GetRfqProcessesListQuery(status, page, pageSize), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RfqProcessDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetRfqProcessByIdQuery(id), cancellationToken));
    }

    /// <summary>Kurulum adım 1/4: temel bilgiler.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Create(CreateRfqProcessRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(
            new CreateRfqProcessCommand(request.Title, request.OwnerCompanyId, request.CurrencyId, request.DueDate),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Kurulum adım 2/4: kalemler.</summary>
    [HttpPost("{id:guid}/line-items")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> AddLineItem(Guid id, AddRfqLineItemRequest request, CancellationToken cancellationToken)
    {
        UnitOfMeasure unitOfMeasure;
        bool parsed = Enum.TryParse(request.UnitOfMeasure, true, out unitOfMeasure);

        if (!parsed)
        {
            return BadRequest($"Geçersiz UnitOfMeasure: {request.UnitOfMeasure}");
        }

        Guid lineItemId = await _sender.Send(
            new AddRfqLineItemCommand(id, request.ProductId, request.Description, request.Quantity, unitOfMeasure, request.SortOrder),
            cancellationToken);

        return Ok(lineItemId);
    }

    /// <summary>Kurulum adım 3/4: tedarikçi davetleri.</summary>
    [HttpPost("{id:guid}/invitations")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> InviteCompanies(Guid id, InviteCompaniesToRfqRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new InviteCompaniesToRfqCommand(id, request.CompanyIds), cancellationToken);
        return NoContent();
    }

    /// <summary>Kurulum adım 4/4: gözden geçir ve yayınla.</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new PublishRfqProcessCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/invitations/respond")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<IActionResult> RespondToInvitation(Guid id, RespondToRfqInvitationRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new RespondToRfqInvitationCommand(id, request.Accept, request.DeclineReason), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/quotes")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<ActionResult<Guid>> SubmitQuote(Guid id, SubmitRfqQuoteRequest request, CancellationToken cancellationToken)
    {
        var lineItems = request.LineItems.Select(l => new Application.Rfq.Commands.SubmitRfqQuote.RfqQuoteLineItemInput(
            l.RfqLineItemId, l.UnitPrice, l.CurrencyId, l.LeadTimeDays, l.Notes)).ToList();

        Guid quoteId = await _sender.Send(new SubmitRfqQuoteCommand(id, request.ValidUntil, request.Notes, lineItems), cancellationToken);
        return Ok(quoteId);
    }

    [HttpGet("{id:guid}/quotes")]
    public async Task<ActionResult<IReadOnlyCollection<RfqQuoteDto>>> GetQuotes(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetRfqQuotesQuery(id), cancellationToken));
    }

    [HttpGet("{id:guid}/comparison-matrix")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<IReadOnlyCollection<RfqComparisonRowDto>>> GetComparisonMatrix(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetRfqComparisonMatrixQuery(id), cancellationToken));
    }

    [HttpGet("{id:guid}/ai-recommendation")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<RfqAiRecommendationDto>> GetAiRecommendation(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetRfqAiRecommendationQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new CloseRfqProcessCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/award")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<IActionResult> Award(Guid id, AwardRfqProcessRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new AwardRfqProcessCommand(id, request.WinningQuoteId, request.Notes), cancellationToken);
        return NoContent();
    }
}
