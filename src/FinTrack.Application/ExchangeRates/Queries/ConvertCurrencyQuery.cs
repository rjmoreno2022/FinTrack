using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Application.ExchangeRates.DTOs;
using FinTrack.Domain.Enums;
using FinTrack.Domain.ValueObjects;
using MediatR;

namespace FinTrack.Application.ExchangeRates.Queries;

public record ConvertCurrencyQuery(
    decimal Amount,
    string FromCurrency,
    string ToCurrency
) : IRequest<Result<CurrencyConversionResultDto>>;

public class ConvertCurrencyHandler : IRequestHandler<ConvertCurrencyQuery, Result<CurrencyConversionResultDto>>
{
    private readonly IMediator _mediator;

    public ConvertCurrencyHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<CurrencyConversionResultDto>> Handle(ConvertCurrencyQuery request, CancellationToken ct)
    {
        if (request.Amount < 0)
            return Result<CurrencyConversionResultDto>.Fail("El monto no puede ser negativo");

        if (!Enum.TryParse<Currency>(request.FromCurrency, true, out var from))
            return Result<CurrencyConversionResultDto>.Fail($"Moneda origen inválida: {request.FromCurrency}");

        if (!Enum.TryParse<Currency>(request.ToCurrency, true, out var to))
            return Result<CurrencyConversionResultDto>.Fail($"Moneda destino inválida: {request.ToCurrency}");

        var rateResult = await _mediator.Send(new GetLatestExchangeRateQuery(request.FromCurrency, request.ToCurrency), ct);
        if (!rateResult.IsSuccess || rateResult.Value == null)
            return Result<CurrencyConversionResultDto>.Fail(rateResult.Error ?? "Error al obtener la tasa de conversión");

        var rate = rateResult.Value.Rate;
        var money = new Money(request.Amount, from);
        var convertedMoney = money.ConvertTo(to, rate);

        return Result<CurrencyConversionResultDto>.Ok(new CurrencyConversionResultDto
        {
            OriginalAmount = request.Amount,
            FromCurrency = from.ToString(),
            ConvertedAmount = Math.Round(convertedMoney.Amount, 2),
            ToCurrency = to.ToString(),
            RateUsed = rate,
            Source = rateResult.Value.Source,
            Date = DateTime.UtcNow
        });
    }
}
