using System;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;

namespace FinTrack.Domain.Entities;

/// <summary>
/// Registra el tipo de cambio entre dos monedas en una fecha y fuente específicas.
/// </summary>
public class ExchangeRate : Entity
{
    public Currency FromCurrency { get; private set; }
    public Currency ToCurrency { get; private set; }
    public decimal Rate { get; private set; }
    public DateTime Date { get; private set; }
    public RateSource Source { get; private set; }
    public bool IsActive { get; private set; }

    // EF Core requiere constructor vacío
    private ExchangeRate() { }

    public ExchangeRate(Currency from, Currency to, decimal rate, RateSource source)
    {
        if (rate <= 0)
            throw new DomainException("La tasa de cambio debe ser mayor a cero");
        if (from == to)
            throw new DomainException("Las monedas origen y destino deben ser diferentes");

        FromCurrency = from;
        ToCurrency = to;
        Rate = rate;
        Date = DateTime.UtcNow;
        Source = source;
        IsActive = true;
    }

    public decimal Convert(decimal amount) => amount * Rate;

    public void UpdateRate(decimal newRate)
    {
        if (newRate <= 0)
            throw new DomainException("La tasa de cambio debe ser mayor a cero");
        Rate = newRate;
        Date = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
