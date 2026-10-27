using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Invitations.Commands;
using ProcureLive.Application.Invitations.Queries;
using ProcureLive.Contracts.Auth;
using ProcureLive.Contracts.Invitations;

namespace ProcureLive.WebApi.Controllers;

/// <summary>
/// Davet linki akışı (/invite?token=...): karşılama bilgisi ve kayıt anonimdir — token'ın kendisi
/// 256-bit gizli anahtardır; sahiplenme (claim) için aynı cariye bağlı bir tedarikçi oturumu gerekir.
/// </summary>
[ApiController]
[Route("api/invite")]
public sealed class InviteController : ControllerBase
{
    private readonly ISender _sender;

    public InviteController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<InviteInfoDto>> GetInfo(string token, CancellationToken cancellationToken)
    {
        InviteInfoDto result = await _sender.Send(new GetInviteInfoQuery(token), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{token}/register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Register(
        string token, RegisterWithInviteRequest request, CancellationToken cancellationToken)
    {
        AuthResultDto result = await _sender.Send(
            new RegisterWithInviteCommand(token, request.Email, request.Password, request.FirstName, request.LastName),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{token}/claim")]
    [Authorize(Policy = Policies.RequireSupplier)]
    public async Task<ActionResult<ClaimInviteResultDto>> Claim(string token, CancellationToken cancellationToken)
    {
        ClaimInviteResultDto result = await _sender.Send(new ClaimInviteCommand(token), cancellationToken);
        return Ok(result);
    }
}
