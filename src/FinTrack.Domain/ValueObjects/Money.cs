using System;
using System.Collections.Generic;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;

namespace FinTrack.Domain.ValueObjects;

/// <summary>
/// Representa un valor monetario inmutable con su respectiva moneda.
/// </summary>
public class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new DomainException("El monto no puede ser negativo");

        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("No se pueden sumar montos de diferentes monedas");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("No se pueden restar montos de diferentes monedas");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money ConvertTo(Currency targetCurrency, decimal rate)
    {
        if (rate <= 0)
            throw new DomainException("La tasa de cambio para la conversión debe ser mayor a cero");

        if (Currency == targetCurrency)
            return this;

        var convertedAmount = Amount * rate;
        return new Money(convertedAmount, targetCurrency);
    }

    public override string ToString() => $"{Currency} {Amount:N2}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
