using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Subscriptions.Commands;
using ProcureLive.Application.Subscriptions.Queries;
using ProcureLive.Contracts.Subscriptions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/subscription")]
[Authorize]
public sealed class SubscriptionController : ControllerBase
{
    private readonly ISender _sender;

    public SubscriptionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("plans")]
    public async Task<ActionResult<SubscriptionPlansDto>> GetPlans(CancellationToken cancellationToken)
    {
        SubscriptionPlansDto result = await _sender.Send(new GetSubscriptionPlansQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<ActionResult<MySubscriptionDto>> GetMine(CancellationToken cancellationToken)
    {
        MySubscriptionDto result = await _sender.Send(new GetMySubscriptionQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Demo satın alım: mock ödeme servisi; kart için yalnızca son 4 hane + kart sahibi alınır.</summary>
    [HttpPost("purchase")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<ActionResult<PurchaseResultDto>> Purchase(
        PurchaseSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.SeatBand, true, out SeatBand seatBand) || !Enum.IsDefined(seatBand))
        {
            return BadRequest($"Geçersiz paket: {request.SeatBand}");
        }

        PurchaseResultDto result = await _sender.Send(
            new PurchaseSubscriptionCommand(seatBand, request.CardHolder, request.CardLast4), cancellationToken);

        return Ok(result);
    }
}
