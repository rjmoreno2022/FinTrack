using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Accounts.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Accounts;

public class TransferFundsHandlerTests
{
    private readonly Mock<IRepository<Account>> _accountRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly TransferFundsHandler _handler;

    public TransferFundsHandlerTests()
    {
        _handler = new TransferFundsHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldTransferFundsAndLinkTransactions()
    {
        // Arrange
        var source = new Account("Banesco USD", AccountType.Bank, Currency.USD, initialBalance: 1000m);
        var destination = new Account("Efectivo USD", AccountType.Cash, Currency.USD, initialBalance: 100m);

        _accountRepoMock.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _accountRepoMock.Setup(r => r.GetByIdAsync(destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destination);

        var command = new TransferFundsCommand(
            source.Id,
            destination.Id,
            300m,
            Description: "Retiro para gastos menores"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        source.Balance.Should().Be(700m);
        destination.Balance.Should().Be(400m);

        source.Transactions.Should().HaveCount(1);
        destination.Transactions.Should().HaveCount(1);

        var outTx = source.Transactions.First();
        var inTx = destination.Transactions.First();

        outTx.Amount.Should().Be(300m);
        outTx.Type.Should().Be(TransactionType.Transfer);
        outTx.IsIncomingTransfer.Should().BeFalse();
        outTx.TransferAccountId.Should().Be(destination.Id);
        outTx.RelatedTransactionId.Should().Be(inTx.Id);

        inTx.Amount.Should().Be(300m);
        inTx.Type.Should().Be(TransactionType.Transfer);
        inTx.IsIncomingTransfer.Should().BeTrue();
        inTx.TransferAccountId.Should().Be(source.Id);
        inTx.RelatedTransactionId.Should().Be(outTx.Id);

        _accountRepoMock.Verify(r => r.Update(source), Times.Once);
        _accountRepoMock.Verify(r => r.Update(destination), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomDestinationAmount_ShouldCreditCustomAmount()
    {
        // Arrange: USD to VES transfer
        var source = new Account("Zelle USD", AccountType.Bank, Currency.USD, initialBalance: 500m);
        var destination = new Account("PagoMovil VES", AccountType.Bank, Currency.VES, initialBalance: 0m);

        _accountRepoMock.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _accountRepoMock.Setup(r => r.GetByIdAsync(destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destination);

        var command = new TransferFundsCommand(
            source.Id,
            destination.Id,
            100m,
            DestinationAmount: 6000m,
            Description: "Cambio USD a VES"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        source.Balance.Should().Be(400m);
        destination.Balance.Should().Be(6000m);
    }

    [Fact]
    public async Task Handle_WhenSameAccount_ShouldFail()
    {
        var accountId = Guid.NewGuid();
        var command = new TransferFundsCommand(accountId, accountId, 100m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("diferente");
    }

    [Fact]
    public async Task Handle_WhenInsufficientFunds_ShouldFail()
    {
        var source = new Account("Banesco", AccountType.Bank, Currency.USD, initialBalance: 50m);
        var destination = new Account("Efectivo", AccountType.Cash, Currency.USD, initialBalance: 0m);

        _accountRepoMock.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _accountRepoMock.Setup(r => r.GetByIdAsync(destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destination);

        var command = new TransferFundsCommand(source.Id, destination.Id, 100m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Fondos insuficientes");
    }

    [Fact]
    public async Task Handle_WhenDestinationInactive_ShouldFail()
    {
        var source = new Account("Banesco", AccountType.Bank, Currency.USD, initialBalance: 500m);
        var destination = new Account("Cerrada", AccountType.Cash, Currency.USD, initialBalance: 0m);
        destination.Deactivate();

        _accountRepoMock.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _accountRepoMock.Setup(r => r.GetByIdAsync(destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destination);

        var command = new TransferFundsCommand(source.Id, destination.Id, 100m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inactiva");
    }
}
