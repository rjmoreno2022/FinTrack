using System;
using FinTrack.Application.Budgets.Commands;
using FinTrack.Domain.Enums;
using FluentValidation;

namespace FinTrack.Application.Budgets.Validators;

public class CreateBudgetValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del presupuesto es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.LimitAmount)
            .GreaterThan(0).WithMessage("El límite de gasto debe ser mayor a cero");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("La moneda es requerida")
            .Must(c => Enum.TryParse<Currency>(c, true, out _))
            .WithMessage("Moneda inválida. Valores válidos: USD, VES, USDT, EUR");

        RuleFor(x => x.Period)
            .NotEmpty().WithMessage("El período es requerido")
            .Must(p => Enum.TryParse<BudgetPeriod>(p, true, out _))
            .WithMessage("Período inválido. Valores válidos: Weekly, Monthly, Annual, Custom");

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate).WithMessage("La fecha de inicio debe ser anterior a la fecha de fin");
    }
}
