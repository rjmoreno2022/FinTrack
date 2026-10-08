using System;
using FinTrack.Application.Accounts.Commands;
using FinTrack.Domain.Enums;
using FluentValidation;

namespace FinTrack.Application.Accounts.Validators;

public class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la cuenta es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre no puede contener solo espacios");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("El tipo de cuenta es requerido")
            .Must(t => Enum.TryParse<AccountType>(t, true, out _))
            .WithMessage("Tipo de cuenta inválido. Valores válidos: Bank, Cash, Crypto, Wallet, Savings");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("La moneda es requerida")
            .Must(c => Enum.TryParse<Currency>(c, true, out _))
            .WithMessage("Moneda inválida. Valores válidos: USD, VES, USDT, EUR");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
