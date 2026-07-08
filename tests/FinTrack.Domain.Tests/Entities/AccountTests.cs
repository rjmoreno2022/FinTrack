using System;
using System.Linq;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FinTrack.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FinTrack.Domain.Tests.Entities;

public class AccountTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateAccountWithZeroBalance()
    {
        // Arrange & Act
        var account = new Account("Banesco VES", AccountType.Bank, Currency.VES, "Cuenta corriente");

        // Assert
        account.Name.Should().Be("Banesco VES");
        account.Type.Should().Be(AccountType.Bank);
        account.Currency.Should().Be(Currency.VES);
        account.Balance.Should().Be(0);
        account.Description.Should().Be("Cuenta corriente");
        account.IsActive.Should().BeTrue();
        account.Transactions.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        Action act = () => new Account(invalidName!, AccountType.Bank, Currency.USD);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("El nombre de la cuenta es requerido");
    }

    [Fact]
    public void AddTransaction_Income_ShouldIncreaseBalanceAndAddTransaction()
    {
        // Arrange
        var account = new Account("Cash USD", AccountType.Cash, Currency.USD);

        // Act
        var transaction = account.AddTransaction(
            TransactionType.Income,
            150m,
            "Freelance payment",
            DateTime.UtcNow
        );

        // Assert
        account.Balance.Should().Be(150m);
        account.Transactions.Should().ContainSingle()
            .Which.Should().Be(transaction);
        
        transaction.Type.Should().Be(TransactionType.Income);
        transaction.Amount.Should().Be(150m);
        transaction.Currency.Should().Be(Currency.USD);
        transaction.Description.Should().Be("Freelance payment");
    }

    [Fact]
    public void AddTransaction_Expense_ShouldDecreaseBalanceAndAddTransaction()
    {
        // Arrange
        var account = new Account("Cash USD", AccountType.Cash, Currency.USD);
        account.AddTransaction(TransactionType.Income, 200m, "Initial deposit", DateTime.UtcNow);

        // Act
        account.AddTransaction(
            TransactionType.Expense,
            80m,
            "Groceries",
            DateTime.UtcNow
        );

        // Assert
        account.Balance.Should().Be(120m);
        account.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public void AddTransaction_ToInactiveAccount_ShouldThrowDomainException()
    {
        // Arrange
        var account = new Account("Closed Account", AccountType.Bank, Currency.USD);
        account.Deactivate();

        // Act
        Action act = () => account.AddTransaction(TransactionType.Income, 100m, "Deposit", DateTime.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("No se pueden agregar transacciones a una cuenta inactiva");
    }

    [Fact]
    public void AddTransaction_ShouldRaiseTransactionCreatedEvent()
    {
        // Arrange
        var account = new Account("Cash USD", AccountType.Cash, Currency.USD);

        // Act
        var transaction = account.AddTransaction(TransactionType.Income, 100m, "Deposit", DateTime.UtcNow);

        // Assert
        account.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TransactionCreatedEvent>();

        var ev = (TransactionCreatedEvent)account.DomainEvents.First();
        ev.AccountId.Should().Be(account.Id);
        ev.TransactionId.Should().Be(transaction.Id);
        ev.Amount.Should().Be(100m);
        ev.Type.Should().Be(TransactionType.Income);
    }

    [Fact]
    public void UpdateInfo_WithValidParameters_ShouldUpdateAccount()
    {
        // Arrange
        var account = new Account("Old Name", AccountType.Bank, Currency.USD, "Old Desc");

        // Act
        account.UpdateInfo("New Name", "New Desc");

        // Assert
        account.Name.Should().Be("New Name");
        account.Description.Should().Be("New Desc");
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateInfo_WithInvalidName_ShouldThrowDomainException()
    {
        // Arrange
        var account = new Account("Valid Name", AccountType.Bank, Currency.USD);

        // Act
        Action act = () => account.UpdateInfo("", null);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("El nombre de la cuenta es requerido");
    }
}
