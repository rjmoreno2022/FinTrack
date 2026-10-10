using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Transactions.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Transactions;

public class DeleteTransactionHandlerTests
{
    private readonly Mock<IRepository<Account>> _accountRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task DeleteTransaction_WhenAccountAndTransactionExist_ShouldRemoveAndReturnSuccess()
    {
        // Arrange
        var account = new Account("Test Account", AccountType.Bank, Currency.USD);
        var transaction = account.AddTransaction(TransactionType.Income, 500m, "Sueldo", DateTime.UtcNow);

        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new DeleteTransactionHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
        var command = new DeleteTransactionCommand(transaction.Id, account.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(0m);
        _accountRepoMock.Verify(r => r.Update(account), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTransaction_WhenAccountNotFound_ShouldFail()
    {
        // Arrange
        _accountRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var handler = new DeleteTransactionHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
        var command = new DeleteTransactionCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("No se encontró");
    }
}
