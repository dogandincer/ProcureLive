using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Finance.Commands.CreateCurrency;
using ProcureLive.Application.Finance.Commands.CreateExchangeRate;
using ProcureLive.Application.Finance.Queries.GetCurrenciesList;
using ProcureLive.Application.Finance.Queries.GetExchangeRates;
using ProcureLive.Contracts.Finance;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/currencies")]
[Authorize]
public sealed class CurrenciesController : ControllerBase
{
    private readonly ISender _sender;

    public CurrenciesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CurrencyDto>>> GetList(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CurrencyDto> result = await _sender.Send(new GetCurrenciesListQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> Create(CreateCurrencyRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(
            new CreateCurrencyCommand(request.Code, request.Name, request.Symbol, request.IsBaseCurrency), cancellationToken);

        return Ok(id);
    }

    [HttpGet("exchange-rates")]
    public async Task<ActionResult<IReadOnlyCollection<ExchangeRateDto>>> GetExchangeRates(
        [FromQuery] Guid? currency,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ExchangeRateDto> result = await _sender.Send(new GetExchangeRatesQuery(currency, date), cancellationToken);
        return Ok(result);
    }

    [HttpPost("exchange-rates")]
    [Authorize(Policy = Policies.RequireInternalUser)]
    public async Task<ActionResult<Guid>> CreateExchangeRate(CreateExchangeRateRequest request, CancellationToken cancellationToken)
    {
        Guid id = await _sender.Send(
            new CreateExchangeRateCommand(request.CurrencyId, request.RateDate, request.RateToBase, request.Source), cancellationToken);

        return Ok(id);
    }
}
