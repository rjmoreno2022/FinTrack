using System;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FinTrack.Domain.Tests.Entities;

public class DebtTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateDebt()
    {
        var debt = new Debt("Banco Mercantil", 500m, Currency.USD, DebtType.Receivable);

        debt.Creditor.Should().Be("Banco Mercantil");
        debt.OriginalAmount.Should().Be(500m);
        debt.RemainingAmount.Should().Be(500m);
        debt.Status.Should().Be(DebtStatus.Active);
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateDebt()
    {
        var debt = new Debt("Amigo", 200m, Currency.USD, DebtType.Owed);
        var dueDate = DateTime.UtcNow.AddMonths(1);

        debt.Update("Amigo Juan", "Préstamo personal", dueDate, 5m);

        debt.Creditor.Should().Be("Amigo Juan");
        debt.Description.Should().Be("Préstamo personal");
        debt.DueDate.Should().Be(dueDate);
        debt.InterestRate.Should().Be(5m);
        debt.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithEmptyCreditor_ShouldThrowDomainException()
    {
        var debt = new Debt("Amigo", 200m, Currency.USD, DebtType.Owed);

        var act = () => debt.Update("   ");

        act.Should().Throw<DomainException>()
            .WithMessage("El acreedor es requerido");
    }

    [Fact]
    public void Update_WithNegativeInterest_ShouldThrowDomainException()
    {
        var debt = new Debt("Amigo", 200m, Currency.USD, DebtType.Owed);

        var act = () => debt.Update("Amigo", interestRate: -2m);

        act.Should().Throw<DomainException>()
            .WithMessage("La tasa de interés no puede ser negativa");
    }
}
