using FluentValidation;
using FinTrack.Application.Budgets.Commands;

namespace FinTrack.Application.Budgets.Validators;

public class UpdateBudgetValidator : AbstractValidator<UpdateBudgetCommand>
{
    public UpdateBudgetValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID del presupuesto es requerido");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del presupuesto es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.LimitAmount)
            .GreaterThan(0).WithMessage("El límite debe ser mayor a cero");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("La fecha de inicio es requerida");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("La fecha de fin es requerida")
            .GreaterThan(x => x.StartDate).WithMessage("La fecha de fin debe ser posterior a la fecha de inicio");
    }
}
