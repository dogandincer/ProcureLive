using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Search.Queries;
using ProcureLive.Contracts.Search;
using ProcureLive.Domain.Enums;

namespace ProcureLive.WebApi.Controllers;

/// <summary>Plus detaylı arama. Plus kontrolü handler'dadır (Standart üye → 403, code: PLUS_REQUIRED).</summary>
[ApiController]
[Route("api/search")]
[Authorize]
public sealed class SearchController : ControllerBase
{
    private readonly ISender _sender;

    public SearchController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResultDto>> Search(
        [FromQuery] string? q,
        [FromQuery] string? scope,
        [FromQuery] string? auctionType,
        [FromQuery] int? minParticipants,
        [FromQuery] int? maxParticipants,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? ownerCompanyId,
        [FromQuery] string? sort,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        AuctionType? type = null;
        if (!string.IsNullOrWhiteSpace(auctionType))
        {
            if (!Enum.TryParse(auctionType, true, out AuctionType parsed) || !Enum.IsDefined(parsed))
            {
                return BadRequest($"Geçersiz AuctionType: {auctionType}");
            }

            type = parsed;
        }

        SearchResultDto result = await _sender.Send(
            new SearchQuery(q, scope, type, minParticipants, maxParticipants, productId, ownerCompanyId, sort, take),
            cancellationToken);

        return Ok(result);
    }
}
