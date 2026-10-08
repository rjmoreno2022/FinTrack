using System;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FinTrack.Domain.Tests.Entities;

public class TransactionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateTransaction()
    {
        var accountId = Guid.NewGuid();
        var date = DateTime.UtcNow;

        var transaction = new Transaction(
            accountId,
            TransactionType.Expense,
            50m,
            Currency.USD,
            "Almuerzo",
            date,
            null,
            "comida"
        );

        transaction.AccountId.Should().Be(accountId);
        transaction.Type.Should().Be(TransactionType.Expense);
        transaction.Amount.Should().Be(50m);
        transaction.Currency.Should().Be(Currency.USD);
        transaction.Description.Should().Be("Almuerzo");
        transaction.Date.Should().Be(date);
        transaction.Tags.Should().Be("comida");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_WithNonPositiveAmount_ShouldThrowDomainException(decimal amount)
    {
        var act = () => new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            amount,
            Currency.USD,
            "Invalido",
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>()
            .WithMessage("El monto debe ser mayor a cero");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyDescription_ShouldThrowDomainException(string? description)
    {
        var act = () => new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            20m,
            Currency.USD,
            description!,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>()
            .WithMessage("La descripción es requerida");
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateFields()
    {
        var transaction = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            30m,
            Currency.USD,
            "Original",
            DateTime.UtcNow.AddDays(-2)
        );

        var newDate = DateTime.UtcNow;
        var categoryId = Guid.NewGuid();

        transaction.Update("Modificado", newDate, categoryId, "nuevo-tag");

        transaction.Description.Should().Be("Modificado");
        transaction.Date.Should().Be(newDate);
        transaction.CategoryId.Should().Be(categoryId);
        transaction.Tags.Should().Be("nuevo-tag");
        transaction.UpdatedAt.Should().NotBeNull();
    }
}
