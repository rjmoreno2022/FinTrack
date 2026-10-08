using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Models;
using FinTrack.Application.ExchangeRates.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using MediatR;

namespace FinTrack.Application.ExchangeRates.Commands;

public record SyncOfficialRatesCommand : IRequest<Result<OfficialRatesDto>>;

public class SyncOfficialRatesCommandHandler : IRequestHandler<SyncOfficialRatesCommand, Result<OfficialRatesDto>>
{
    private readonly IBcvRateProvider _bcvRateProvider;
    private readonly IRepository<ExchangeRate> _rateRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SyncOfficialRatesCommandHandler(
        IBcvRateProvider bcvRateProvider,
        IRepository<ExchangeRate> rateRepo,
        IUnitOfWork unitOfWork)
    {
        _bcvRateProvider = bcvRateProvider;
        _rateRepo = rateRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OfficialRatesDto>> Handle(SyncOfficialRatesCommand request, CancellationToken ct)
    {
        var liveRates = await _bcvRateProvider.GetLiveOfficialRatesAsync(ct);
        if (liveRates == null || liveRates.UsdRate <= 0 || liveRates.EurRate <= 0)
        {
            return Result<OfficialRatesDto>.Fail("No fue posible obtener las cotizaciones oficiales del BCV en este momento.");
        }

        var allRates = await _rateRepo.ListAllAsync(ct);

        // 1. Desactivar tasas previas para USD -> VES y registrar nueva tasa oficial
        var existingActiveUsd = allRates
            .Where(r => r.FromCurrency == Currency.USD && r.ToCurrency == Currency.VES && r.IsActive)
            .ToList();

        foreach (var oldRate in existingActiveUsd)
        {
            oldRate.Deactivate();
            _rateRepo.Update(oldRate);
        }

        var newUsdRate = new ExchangeRate(Currency.USD, Currency.VES, liveRates.UsdRate, RateSource.BCV);
        await _rateRepo.AddAsync(newUsdRate, ct);

        // 2. Desactivar tasas previas para EUR -> VES y registrar nueva tasa oficial
        var existingActiveEur = allRates
            .Where(r => r.FromCurrency == Currency.EUR && r.ToCurrency == Currency.VES && r.IsActive)
            .ToList();

        foreach (var oldRate in existingActiveEur)
        {
            oldRate.Deactivate();
            _rateRepo.Update(oldRate);
        }

        var newEurRate = new ExchangeRate(Currency.EUR, Currency.VES, liveRates.EurRate, RateSource.BCV);
        await _rateRepo.AddAsync(newEurRate, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result<OfficialRatesDto>.Ok(liveRates);
    }
}
