using System;
using System.Threading.Tasks;
using FinTrack.Application.ExchangeRates.Commands;
using FinTrack.Application.ExchangeRates.DTOs;
using FinTrack.Application.ExchangeRates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExchangeRatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExchangeRatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetExchangeRatesListQuery(activeOnly));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] string from = "USD", [FromQuery] string to = "VES")
    {
        var result = await _mediator.Send(new GetLatestExchangeRateQuery(from, to));
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExchangeRateRequest request)
    {
        var command = new CreateExchangeRateCommand(
            request.FromCurrency,
            request.ToCurrency,
            request.Rate,
            request.Source
        );

        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetLatest), new { from = request.FromCurrency, to = request.ToCurrency }, new { id = result.Value })
            : BadRequest(new { error = result.Error, errors = result.Errors });
    }

    [HttpGet("convert")]
    public async Task<IActionResult> Convert(
        [FromQuery] decimal amount,
        [FromQuery] string from = "USD",
        [FromQuery] string to = "VES")
    {
        var result = await _mediator.Send(new ConvertCurrencyQuery(amount, from, to));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("bcv-live")]
    public async Task<IActionResult> GetBcvLive()
    {
        var result = await _mediator.Send(new GetBcvLiveRateQuery());
        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, new { error = result.Error });
    }

    [HttpGet("official-live")]
    public async Task<IActionResult> GetOfficialLive()
    {
        var result = await _mediator.Send(new GetLiveOfficialRatesQuery());
        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, new { error = result.Error });
    }

    [HttpPost("sync-official")]
    public async Task<IActionResult> SyncOfficial()
    {
        var result = await _mediator.Send(new SyncOfficialRatesCommand());
        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, new { error = result.Error });
    }
}
