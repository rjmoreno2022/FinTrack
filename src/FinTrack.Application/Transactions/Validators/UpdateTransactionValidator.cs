using FluentValidation;
using FinTrack.Application.Transactions.Commands;

namespace FinTrack.Application.Transactions.Validators;

public class UpdateTransactionValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de la transacción es requerido");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es requerida")
            .MaximumLength(200).WithMessage("La descripción no puede exceder 200 caracteres");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha es requerida");

        RuleFor(x => x.Tags)
            .MaximumLength(100).WithMessage("Las etiquetas no pueden exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Tags));
    }
}
