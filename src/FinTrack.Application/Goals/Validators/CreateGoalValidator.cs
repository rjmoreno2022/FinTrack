using System;
using FinTrack.Application.Goals.Commands;
using FinTrack.Domain.Enums;
using FluentValidation;

namespace FinTrack.Application.Goals.Validators;

public class CreateGoalValidator : AbstractValidator<CreateGoalCommand>
{
    public CreateGoalValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la meta es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0).WithMessage("El monto objetivo debe ser mayor a cero");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("La moneda es requerida")
            .Must(c => Enum.TryParse<Currency>(c, true, out _))
            .WithMessage("Moneda inválida. Valores válidos: USD, VES, USDT, EUR");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("La prioridad es requerida")
            .Must(p => Enum.TryParse<GoalPriority>(p, true, out _))
            .WithMessage("Prioridad inválida. Valores válidos: Low, Medium, High, Critical");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
