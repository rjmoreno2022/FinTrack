using FinTrack.Application.Goals.Commands;
using FluentValidation;

namespace FinTrack.Application.Goals.Validators;

public class ContributeGoalValidator : AbstractValidator<ContributeGoalCommand>
{
    public ContributeGoalValidator()
    {
        RuleFor(x => x.GoalId)
            .NotEmpty().WithMessage("El identificador de la meta es requerido");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto de la contribución debe ser mayor a cero");
    }
}
