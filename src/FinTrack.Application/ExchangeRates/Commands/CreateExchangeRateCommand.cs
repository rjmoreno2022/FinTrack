using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Models;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.ExchangeRates.Commands;

public record CreateExchangeRateCommand(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    string Source
) : IRequest<Result<Guid>>;

public class CreateExchangeRateHandler : IRequestHandler<CreateExchangeRateCommand, Result<Guid>>
{
    private readonly IRepository<ExchangeRate> _rateRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateExchangeRateHandler(IRepository<ExchangeRate> rateRepo, IUnitOfWork unitOfWork)
    {
        _rateRepo = rateRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateExchangeRateCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<Currency>(request.FromCurrency, true, out var fromCurrency))
            return Result<Guid>.Fail($"Moneda origen inválida: {request.FromCurrency}");

        if (!Enum.TryParse<Currency>(request.ToCurrency, true, out var toCurrency))
            return Result<Guid>.Fail($"Moneda destino inválida: {request.ToCurrency}");

        if (fromCurrency == toCurrency)
            return Result<Guid>.Fail("Las monedas de origen y destino deben ser diferentes");

        if (request.Rate <= 0)
            return Result<Guid>.Fail("La tasa de cambio debe ser mayor a cero");

        if (!Enum.TryParse<RateSource>(request.Source, true, out var source))
            return Result<Guid>.Fail($"Fuente de tasa inválida: {request.Source}");

        // Desactivar tasas anteriores del mismo par
        var allRates = await _rateRepo.ListAllAsync(ct);
        var existingActive = allRates
            .Where(r => r.FromCurrency == fromCurrency && r.ToCurrency == toCurrency && r.IsActive)
            .ToList();

        foreach (var oldRate in existingActive)
        {
            oldRate.Deactivate();
            _rateRepo.Update(oldRate);
        }

        var newRate = new ExchangeRate(fromCurrency, toCurrency, request.Rate, source);
        await _rateRepo.AddAsync(newRate, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Ok(newRate.Id);
    }
}
