using System;

namespace FinTrack.Application.ExchangeRates.DTOs;

public record BcvRateResultDto(
    decimal Rate,
    DateTime EffectiveDate,
    string Source,
    bool IsDirectPortal
);
