using FluentValidation;
using FinTrack.Application.Debts.Commands;

namespace FinTrack.Application.Debts.Validators;

public class UpdateDebtValidator : AbstractValidator<UpdateDebtCommand>
{
    public UpdateDebtValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de la deuda es requerido");

        RuleFor(x => x.Creditor)
            .NotEmpty().WithMessage("El acreedor es requerido")
            .MaximumLength(150).WithMessage("El acreedor no puede exceder 150 caracteres");

        RuleFor(x => x.InterestRate)
            .GreaterThanOrEqualTo(0).WithMessage("La tasa de interés no puede ser negativa");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
