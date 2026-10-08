using System;
using System.Collections.Generic;
using System.Linq;
using FinTrack.Application.Accounts.DTOs;

namespace FinTrack.Web.Services;

public class AppState
{
    public event Action? OnChange;

    private List<AccountDto> _accounts = new();
    public IReadOnlyList<AccountDto> Accounts => _accounts.AsReadOnly();

    public string DisplayCurrency { get; private set; } = "USD";
    public decimal ExchangeRateUsdToVes { get; private set; } = 60.0m;
    public decimal ExchangeRateEurToVes { get; private set; } = 70.0m;
    public string RateSource { get; private set; } = "BCV";
    public DateTime? LastRateSyncDate { get; private set; }
    public bool IsRateLoaded { get; private set; } = false;

    public string CurrencySymbol => DisplayCurrency switch
    {
        "VES" => "Bs.",
        "EUR" => "€",
        _ => "$"
    };

    public void SetDisplayCurrency(string currency)
    {
        if (DisplayCurrency != currency)
        {
            DisplayCurrency = currency;
            NotifyStateChanged();
        }
    }

    public void SetExchangeRate(decimal rate, string source)
    {
        ExchangeRateUsdToVes = rate;
        RateSource = source;
        IsRateLoaded = true;
        LastRateSyncDate = DateTime.UtcNow;
        NotifyStateChanged();
    }

    public void SetExchangeRates(decimal usdRate, decimal eurRate, string source, DateTime? effectiveDate = null)
    {
        ExchangeRateUsdToVes = usdRate;
        ExchangeRateEurToVes = eurRate;
        RateSource = source;
        IsRateLoaded = true;
        LastRateSyncDate = effectiveDate ?? DateTime.UtcNow;
        NotifyStateChanged();
    }

    public decimal ConvertToDisplay(decimal amount, string fromCurrency)
    {
        if (string.Equals(fromCurrency, DisplayCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        // Normalizar USDT a USD
        var normalizedFrom = string.Equals(fromCurrency, "USDT", StringComparison.OrdinalIgnoreCase) ? "USD" : fromCurrency.ToUpperInvariant();
        var normalizedDisplay = string.Equals(DisplayCurrency, "USDT", StringComparison.OrdinalIgnoreCase) ? "USD" : DisplayCurrency.ToUpperInvariant();

        if (string.Equals(normalizedFrom, normalizedDisplay, StringComparison.OrdinalIgnoreCase))
            return amount;

        // Convertir origen a VES (como moneda puente)
        decimal amountInVes = normalizedFrom switch
        {
            "USD" => amount * ExchangeRateUsdToVes,
            "EUR" => amount * ExchangeRateEurToVes,
            "VES" => amount,
            _ => amount
        };

        // Convertir de VES a la moneda de destino
        decimal converted = normalizedDisplay switch
        {
            "VES" => amountInVes,
            "USD" => ExchangeRateUsdToVes > 0 ? amountInVes / ExchangeRateUsdToVes : amountInVes,
            "EUR" => ExchangeRateEurToVes > 0 ? amountInVes / ExchangeRateEurToVes : amountInVes,
            _ => amountInVes
        };

        return Math.Round(converted, 2);
    }

    public void SetAccounts(IEnumerable<AccountDto> accounts)
    {
        _accounts = accounts.ToList();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
