using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Auth.Commands.Login;
using ProcureLive.Application.Auth.Commands.RefreshToken;
using ProcureLive.Application.Auth.Commands.Register;
using ProcureLive.Application.Auth.Commands.RegisterSupplier;
using ProcureLive.Application.Auth.Commands.RevokeToken;
using ProcureLive.Application.Auth.Queries.GetCurrentUser;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Auth;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        AuthResultDto result = await _sender.Send(new LoginCommand(request.Email, request.Password, ipAddress), cancellationToken);
        return Ok(result);
    }

    [HttpPost("register")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<AuthResultDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        AuthResultDto result = await _sender.Send(
            new RegisterCommand(request.Email, request.Password, request.FirstName, request.LastName, request.Role, request.CompanyId),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Davetsiz tedarikçi kaydı: cari onay bekler, hiçbir işyerine bağlı değildir; oturum açık döner.</summary>
    [HttpPost("register-supplier")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> RegisterSupplier(RegisterSupplierRequest request, CancellationToken cancellationToken)
    {
        AuthResultDto result = await _sender.Send(
            new RegisterSupplierCommand(
                request.CompanyName, request.TaxNumber, request.TaxOffice, request.Phone,
                request.FirstName, request.LastName, request.Email, request.Password, request.AcceptTerms),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        AuthResultDto result = await _sender.Send(new RefreshTokenCommand(request.RefreshToken, ipAddress), cancellationToken);
        return Ok(result);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken(RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new RevokeTokenCommand(request.RefreshToken), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        string? userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        Guid userId = Guid.Empty;
        bool parsed = userIdValue is not null && Guid.TryParse(userIdValue, out userId);

        if (!parsed)
        {
            return Unauthorized();
        }

        CurrentUserDto result = await _sender.Send(new GetCurrentUserQuery(userId), cancellationToken);
        return Ok(result);
    }
}
