using System;
using FinTrack.Application.ExchangeRates.Commands;
using FinTrack.Domain.Enums;
using FluentValidation;

namespace FinTrack.Application.ExchangeRates.Validators;

public class CreateExchangeRateValidator : AbstractValidator<CreateExchangeRateCommand>
{
    public CreateExchangeRateValidator()
    {
        RuleFor(x => x.FromCurrency)
            .NotEmpty().WithMessage("La moneda de origen es requerida")
            .Must(c => Enum.TryParse<Currency>(c, true, out _))
            .WithMessage("Moneda origen inválida. Valores válidos: USD, VES, USDT, EUR");

        RuleFor(x => x.ToCurrency)
            .NotEmpty().WithMessage("La moneda de destino es requerida")
            .Must(c => Enum.TryParse<Currency>(c, true, out _))
            .WithMessage("Moneda destino inválida. Valores válidos: USD, VES, USDT, EUR");

        RuleFor(x => x)
            .Must(x => !x.FromCurrency.Equals(x.ToCurrency, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Las monedas de origen y destino deben ser diferentes");

        RuleFor(x => x.Rate)
            .GreaterThan(0).WithMessage("La tasa de cambio debe ser mayor a cero");

        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("La fuente es requerida")
            .Must(s => string.Equals(s, "BCV", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Fuente inválida. Solo se admiten fuentes oficiales certificadas: BCV");
    }
}
