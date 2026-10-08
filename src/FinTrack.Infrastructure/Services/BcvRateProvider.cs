using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.ExchangeRates.DTOs;
using Microsoft.Extensions.Logging;

namespace FinTrack.Infrastructure.Services;

public class BcvRateProvider : IBcvRateProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BcvRateProvider> _logger;

    public BcvRateProvider(IHttpClientFactory httpClientFactory, ILogger<BcvRateProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<BcvRateResultDto?> GetLiveRateAsync(CancellationToken cancellationToken = default)
    {
        var official = await GetLiveOfficialRatesAsync(cancellationToken);
        if (official != null && official.UsdRate > 0)
        {
            return new BcvRateResultDto(
                Rate: official.UsdRate,
                EffectiveDate: official.EffectiveDate,
                Source: official.Source,
                IsDirectPortal: official.IsDirectPortal
            );
        }
        return null;
    }

    public async Task<OfficialRatesDto?> GetLiveOfficialRatesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Intento Nivel 1: CDN Oficial DolarVzla (extremadamente rápido, alta disponibilidad y provee USD y EUR juntos)
        try
        {
            var cdnRates = await FetchFromDolarVzlaCdnAsync(cancellationToken);
            if (cdnRates != null && cdnRates.UsdRate > 0 && cdnRates.EurRate > 0)
            {
                _logger.LogInformation("Tasas BCV obtenidas vía CDN oficial: USD {Usd}, EUR {Eur}", cdnRates.UsdRate, cdnRates.EurRate);
                return cdnRates;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo al consultar CDN DolarVzla. Intentando portal directo del BCV...");
        }

        // 2. Intento Nivel 2: Scraping directo al portal oficial del BCV
        try
        {
            var directRates = await FetchDirectFromBcvAsync(cancellationToken);
            if (directRates != null && directRates.UsdRate > 0 && directRates.EurRate > 0)
            {
                _logger.LogInformation("Tasas BCV obtenidas vía portal institucional directo: USD {Usd}, EUR {Eur}", directRates.UsdRate, directRates.EurRate);
                return directRates;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo al consultar portal directo del BCV. Intentando DolarApi...");
        }

        // 3. Intento Nivel 3: DolarApi Venezuela (fallback adicional)
        try
        {
            var dolarApiRates = await FetchFromDolarApiAsync(cancellationToken);
            if (dolarApiRates != null && dolarApiRates.UsdRate > 0 && dolarApiRates.EurRate > 0)
            {
                _logger.LogInformation("Tasas BCV obtenidas vía DolarApi: USD {Usd}, EUR {Eur}", dolarApiRates.UsdRate, dolarApiRates.EurRate);
                return dolarApiRates;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico: todos los proveedores de tasas oficiales BCV fallaron.");
        }

        return null;
    }

    private async Task<OfficialRatesDto?> FetchFromDolarVzlaCdnAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(6);

        var response = await client.GetAsync("https://rates.dolarvzla.com/bcv/current.json", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);

        // Estructura: { ""current"": { ""usd"": 842.2067, ""eur"": 977.8777, ""date"": ""2026-09-15"" } }
        if (doc.RootElement.TryGetProperty("current", out var currentObj))
        {
            decimal usd = 0;
            decimal eur = 0;
            DateTime effectiveDate = DateTime.UtcNow;

            if (currentObj.TryGetProperty("usd", out var usdProp))
                usd = Math.Round(usdProp.GetDecimal(), 2);

            if (currentObj.TryGetProperty("eur", out var eurProp))
                eur = Math.Round(eurProp.GetDecimal(), 2);

            if (currentObj.TryGetProperty("date", out var dateProp) &&
                DateTime.TryParse(dateProp.GetString(), out var parsedDate))
            {
                effectiveDate = parsedDate;
            }

            if (usd > 0 && eur > 0)
            {
                return new OfficialRatesDto(
                    UsdRate: usd,
                    EurRate: eur,
                    EffectiveDate: effectiveDate,
                    Source: "BCV (Oficial)",
                    IsDirectPortal: false
                );
            }
        }

        return null;
    }

    private async Task<OfficialRatesDto?> FetchDirectFromBcvAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("BcvPortalClient");
        var response = await client.GetAsync("https://www.bcv.org.ve/", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var usdRate = ParseBcvCurrency(html, "dolar");
        var eurRate = ParseBcvCurrency(html, "euro");

        if (usdRate.HasValue && usdRate.Value > 0 && eurRate.HasValue && eurRate.Value > 0)
        {
            return new OfficialRatesDto(
                UsdRate: usdRate.Value,
                EurRate: eurRate.Value,
                EffectiveDate: DateTime.UtcNow,
                Source: "BCV (Oficial)",
                IsDirectPortal: true
            );
        }

        return null;
    }

    public static decimal? ParseBcvCurrency(string html, string containerId)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        var pattern = $@"(?i)id=[""']{containerId}[""'][^>]*>.*?<strong>\s*([0-9.,]+)\s*</strong>";
        var match = Regex.Match(html, pattern, RegexOptions.Singleline);
        if (match.Success)
        {
            return ParseVenezuelanDecimal(match.Groups[1].Value);
        }

        return null;
    }

    public static decimal? ParseBcvHtml(string html) => ParseBcvCurrency(html, "dolar");

    public static decimal? ParseVenezuelanDecimal(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var cleaned = rawValue.Trim().Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
        {
            return Math.Round(rate, 2);
        }

        return null;
    }

    private async Task<OfficialRatesDto?> FetchFromDolarApiAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(6);

        decimal usd = 0;
        decimal eur = 0;
        DateTime effectiveDate = DateTime.UtcNow;

        // 1. Obtener USD
        try
        {
            var usdRes = await client.GetAsync("https://ve.dolarapi.com/v1/dolares/oficial", cancellationToken);
            if (usdRes.IsSuccessStatusCode)
            {
                var usdContent = await usdRes.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(usdContent);
                if (doc.RootElement.TryGetProperty("promedio", out var prop))
                    usd = Math.Round(prop.GetDecimal(), 2);
                if (doc.RootElement.TryGetProperty("fechaActualizacion", out var dateProp) &&
                    DateTime.TryParse(dateProp.GetString(), out var d))
                    effectiveDate = d;
            }
        }
        catch { }

        // 2. Obtener EUR
        try
        {
            var eurRes = await client.GetAsync("https://ve.dolarapi.com/v1/euros/oficial", cancellationToken);
            if (eurRes.IsSuccessStatusCode)
            {
                var eurContent = await eurRes.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(eurContent);
                if (doc.RootElement.TryGetProperty("promedio", out var prop))
                    eur = Math.Round(prop.GetDecimal(), 2);
            }
        }
        catch { }

        if (usd > 0 && eur > 0)
        {
            return new OfficialRatesDto(
                UsdRate: usd,
                EurRate: eur,
                EffectiveDate: effectiveDate,
                Source: "BCV (DolarApi CDN)",
                IsDirectPortal: false
            );
        }

        return null;
    }
}
