using FluentValidation;
using FinTrack.Application.Categories.Commands;

namespace FinTrack.Application.Categories.Validators;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la categoría es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("El tipo de categoría es requerido")
            .Must(t => t == "Income" || t == "Expense")
            .WithMessage("El tipo debe ser 'Income' o 'Expense'");

        RuleFor(x => x.Icon)
            .MaximumLength(50).WithMessage("El icono no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Icon));

        RuleFor(x => x.Color)
            .MaximumLength(30).WithMessage("El color no puede exceder 30 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Color));
    }
}
