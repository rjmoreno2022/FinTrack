using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FinTrack.Application.Transactions.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Transactions;

public class CreateTransactionHandlerTests
{
    private readonly Mock<IRepository<Account>> _accountRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateTransactionHandler _handler;

    public CreateTransactionHandlerTests()
    {
        _accountRepoMock = new Mock<IRepository<Account>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateTransactionHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithIncomeTransaction_ShouldAddToAccountAndIncreaseBalance()
    {
        // Arrange
        var account = new Account("Billetera USDT", AccountType.Crypto, Currency.USDT);
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateTransactionCommand(
            account.Id,
            "Income",
            500m,
            "USDT",
            "Cobro de proyecto freelance",
            DateTime.UtcNow
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        account.Balance.Should().Be(500m);
        account.Transactions.Should().HaveCount(1);

        _accountRepoMock.Verify(r => r.Update(account), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExpenseTransaction_WhenSufficientFunds_ShouldDecreaseBalance()
    {
        // Arrange
        var account = new Account("Banesco", AccountType.Bank, Currency.USD);
        account.AddTransaction(TransactionType.Income, 1000m, "Depósito inicial", DateTime.UtcNow);

        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateTransactionCommand(
            account.Id,
            "Expense",
            300m,
            "USD",
            "Supermercado",
            DateTime.UtcNow
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(700m);
        account.Transactions.Should().HaveCount(2);

        _accountRepoMock.Verify(r => r.Update(account), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExpenseTransaction_WhenInsufficientFunds_ShouldReturnFailure()
    {
        // Arrange
        var account = new Account("Efectivo", AccountType.Cash, Currency.USD);
        // Balance inicial es 0

        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var command = new CreateTransactionCommand(
            account.Id,
            "Expense",
            50m,
            "USD",
            "Almuerzo",
            DateTime.UtcNow
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Fondos insuficientes");
        account.Balance.Should().Be(0m);
        account.Transactions.Should().BeEmpty();

        _accountRepoMock.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldReturnFailure()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _accountRepoMock.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var command = new CreateTransactionCommand(
            missingId,
            "Income",
            100m,
            "USD",
            "Pago",
            DateTime.UtcNow
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Cuenta no encontrada");
    }
}
