using FinTrack.Application.Accounts.Commands;
using FluentValidation;

namespace FinTrack.Application.Accounts.Validators;

public class UpdateAccountValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de la cuenta es requerido");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la cuenta es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre no puede contener solo espacios");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
