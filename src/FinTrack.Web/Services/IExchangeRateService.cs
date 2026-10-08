using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrack.Application.ExchangeRates.Commands;
using FinTrack.Application.ExchangeRates.DTOs;
using FinTrack.Application.ExchangeRates.Queries;
using MediatR;

namespace FinTrack.Web.Services;

public interface IExchangeRateService
{
    Task<List<ExchangeRateDto>> GetAllAsync(bool activeOnly = false);
    Task<ExchangeRateDto?> GetLatestRateAsync(string from = "USD", string to = "VES");
    Task<Guid> CreateAsync(CreateExchangeRateRequest request);
    Task<CurrencyConversionResultDto?> ConvertAsync(decimal amount, string from, string to);
    Task<BcvRateResultDto?> GetBcvLiveRateAsync();
    Task<OfficialRatesDto?> SyncOfficialRatesAsync();
    Task<OfficialRatesDto?> GetLiveOfficialRatesAsync();
}

public class ExchangeRateService : IExchangeRateService
{
    private readonly IMediator _mediator;

    public ExchangeRateService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<ExchangeRateDto>> GetAllAsync(bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetExchangeRatesListQuery(activeOnly));
        return result.IsSuccess && result.Value != null
            ? result.Value
            : new List<ExchangeRateDto>();
    }

    public async Task<ExchangeRateDto?> GetLatestRateAsync(string from = "USD", string to = "VES")
    {
        var result = await _mediator.Send(new GetLatestExchangeRateQuery(from, to));
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<Guid> CreateAsync(CreateExchangeRateRequest request)
    {
        var command = new CreateExchangeRateCommand(
            request.FromCurrency,
            request.ToCurrency,
            request.Rate,
            request.Source
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Error al registrar tasa de cambio");

        return result.Value;
    }

    public async Task<CurrencyConversionResultDto?> ConvertAsync(decimal amount, string from, string to)
    {
        var result = await _mediator.Send(new ConvertCurrencyQuery(amount, from, to));
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<BcvRateResultDto?> GetBcvLiveRateAsync()
    {
        var result = await _mediator.Send(new GetBcvLiveRateQuery());
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<OfficialRatesDto?> SyncOfficialRatesAsync()
    {
        var result = await _mediator.Send(new SyncOfficialRatesCommand());
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<OfficialRatesDto?> GetLiveOfficialRatesAsync()
    {
        var result = await _mediator.Send(new GetLiveOfficialRatesQuery());
        return result.IsSuccess ? result.Value : null;
    }
}
