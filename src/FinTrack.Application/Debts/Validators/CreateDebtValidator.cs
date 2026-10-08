using System;
using FinTrack.Application.Debts.Commands;
using FinTrack.Domain.Enums;
using FluentValidation;

namespace FinTrack.Application.Debts.Validators;

public class CreateDebtValidator : AbstractValidator<CreateDebtCommand>
{
    public CreateDebtValidator()
    {
        RuleFor(x => x.Creditor)
            .NotEmpty().WithMessage("El acreedor/deudor es requerido")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres");

        RuleFor(x => x.OriginalAmount)
            .GreaterThan(0).WithMessage("El monto original debe ser mayor a cero");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("La moneda es requerida")
            .Must(c => Enum.TryParse<Currency>(c, true, out _))
            .WithMessage("Moneda inválida. Valores válidos: USD, VES, USDT, EUR");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("El tipo de deuda es requerido")
            .Must(t => Enum.TryParse<DebtType>(t, true, out _))
            .WithMessage("Tipo inválido. Valores válidos: Owed, Receivable");

        RuleFor(x => x.InterestRate)
            .GreaterThanOrEqualTo(0).WithMessage("La tasa de interés no puede ser negativa");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
