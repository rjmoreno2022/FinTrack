namespace FinTrack.Application.ExchangeRates.DTOs;

public class CreateExchangeRateRequest
{
    public string FromCurrency { get; set; } = "USD";
    public string ToCurrency { get; set; } = "VES";
    public decimal Rate { get; set; }
    public string Source { get; set; } = "BCV";
}
