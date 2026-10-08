using System;

namespace FinTrack.Application.ExchangeRates.DTOs;

public class CurrencyConversionResultDto
{
    public decimal OriginalAmount { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public decimal ConvertedAmount { get; set; }
    public string ToCurrency { get; set; } = string.Empty;
    public decimal RateUsed { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
