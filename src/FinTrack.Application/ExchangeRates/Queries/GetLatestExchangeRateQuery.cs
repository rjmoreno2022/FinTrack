using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FinTrack.Application.Common.Models;
using FinTrack.Application.ExchangeRates.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.ExchangeRates.Queries;

public record GetLatestExchangeRateQuery(
    string FromCurrency = "USD",
    string ToCurrency = "VES"
) : IRequest<Result<ExchangeRateDto>>;

public class GetLatestExchangeRateHandler : IRequestHandler<GetLatestExchangeRateQuery, Result<ExchangeRateDto>>
{
    private readonly IRepository<ExchangeRate> _rateRepo;
    private readonly IMapper _mapper;

    public GetLatestExchangeRateHandler(IRepository<ExchangeRate> rateRepo, IMapper mapper)
    {
        _rateRepo = rateRepo;
        _mapper = mapper;
    }

    public async Task<Result<ExchangeRateDto>> Handle(GetLatestExchangeRateQuery request, CancellationToken ct)
    {
        if (!Enum.TryParse<Currency>(request.FromCurrency, true, out var from))
            return Result<ExchangeRateDto>.Fail($"Moneda origen inválida: {request.FromCurrency}");

        if (!Enum.TryParse<Currency>(request.ToCurrency, true, out var to))
            return Result<ExchangeRateDto>.Fail($"Moneda destino inválida: {request.ToCurrency}");

        if (from == to)
        {
            return Result<ExchangeRateDto>.Ok(new ExchangeRateDto
            {
                FromCurrency = from.ToString(),
                ToCurrency = to.ToString(),
                Rate = 1.0m,
                Date = DateTime.UtcNow,
                Source = "Direct",
                IsActive = true
            });
        }

        var rates = await _rateRepo.ListAllAsync(ct);

        // 1. Buscar tasa directa activa
        var direct = rates
            .Where(r => r.FromCurrency == from && r.ToCurrency == to && r.IsActive)
            .OrderByDescending(r => r.Date)
            .FirstOrDefault();

        if (direct != null)
        {
            return Result<ExchangeRateDto>.Ok(_mapper.Map<ExchangeRateDto>(direct));
        }

        // 2. Buscar tasa inversa activa
        var inverse = rates
            .Where(r => r.FromCurrency == to && r.ToCurrency == from && r.IsActive)
            .OrderByDescending(r => r.Date)
            .FirstOrDefault();

        if (inverse != null && inverse.Rate > 0)
        {
            return Result<ExchangeRateDto>.Ok(new ExchangeRateDto
            {
                Id = inverse.Id,
                FromCurrency = from.ToString(),
                ToCurrency = to.ToString(),
                Rate = Math.Round(1.0m / inverse.Rate, 6),
                Date = inverse.Date,
                Source = $"{inverse.Source} (Inversa)",
                IsActive = true
            });
        }

        return Result<ExchangeRateDto>.Fail($"No se encontró una tasa de cambio activa entre {from} y {to}");
    }
}
