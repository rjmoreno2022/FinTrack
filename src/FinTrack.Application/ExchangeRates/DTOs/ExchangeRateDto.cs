using System;

namespace FinTrack.Application.ExchangeRates.DTOs;

public class ExchangeRateDto
{
    public Guid Id { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime Date { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
