using System;

namespace FinTrack.Application.ExchangeRates.DTOs;

public record OfficialRatesDto(
    decimal UsdRate,
    decimal EurRate,
    DateTime EffectiveDate,
    string Source,
    bool IsDirectPortal
);
