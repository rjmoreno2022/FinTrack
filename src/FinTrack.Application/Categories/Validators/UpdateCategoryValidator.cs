using FluentValidation;
using FinTrack.Application.Categories.Commands;

namespace FinTrack.Application.Categories.Validators;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de la categoría es requerido");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la categoría es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.Icon)
            .MaximumLength(50).WithMessage("El icono no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Icon));

        RuleFor(x => x.Color)
            .MaximumLength(30).WithMessage("El color no puede exceder 30 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Color));
    }
}
