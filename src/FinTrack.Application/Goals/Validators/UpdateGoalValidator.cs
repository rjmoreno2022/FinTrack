using FluentValidation;
using FinTrack.Application.Goals.Commands;

namespace FinTrack.Application.Goals.Validators;

public class UpdateGoalValidator : AbstractValidator<UpdateGoalCommand>
{
    public UpdateGoalValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de la meta es requerido");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la meta es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0).WithMessage("El monto objetivo debe ser mayor a cero");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("La prioridad es requerida");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
