using System;
using FinTrack.Application.Debts.Commands;
using FluentValidation;

namespace FinTrack.Application.Debts.Validators;

public class RegisterDebtPaymentValidator : AbstractValidator<RegisterDebtPaymentCommand>
{
    public RegisterDebtPaymentValidator()
    {
        RuleFor(x => x.DebtId)
            .NotEmpty().WithMessage("El identificador de la deuda es requerido");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto del abono debe ser mayor a cero");

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("La fecha del abono no puede ser futura");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("La nota no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
