using System;
using FinTrack.Application.Transactions.Commands;
using FinTrack.Domain.Enums;
using FluentValidation;

namespace FinTrack.Application.Transactions.Validators;

public class CreateTransactionValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("La cuenta es requerida");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a cero");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es requerida")
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("El tipo de transacción es requerido")
            .Must(t => Enum.TryParse<TransactionType>(t, true, out _))
            .WithMessage("Tipo de transacción inválido. Valores válidos: Income, Expense, Transfer");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("La moneda es requerida")
            .Must(c => Enum.TryParse<Currency>(c, true, out _))
            .WithMessage("Moneda inválida. Valores válidos: USD, VES, USDT, EUR");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha es requerida");
    }
}
