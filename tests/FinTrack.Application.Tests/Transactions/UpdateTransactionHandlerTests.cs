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

public class UpdateTransactionHandlerTests
{
    private readonly Mock<IRepository<Account>> _accountRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateTransactionHandler _handler;

    public UpdateTransactionHandlerTests()
    {
        _accountRepoMock = new Mock<IRepository<Account>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateTransactionHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateTransactionAndReturnSuccess()
    {
        var account = new Account("Banesco", AccountType.Bank, Currency.USD);
        var tx = account.AddTransaction(TransactionType.Expense, 40m, "Original", DateTime.UtcNow.AddDays(-1));

        _accountRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var newDate = DateTime.UtcNow;
        var categoryId = Guid.NewGuid();
        var command = new UpdateTransactionCommand(tx.Id, "Modificado", newDate, categoryId, "nuevo");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tx.Description.Should().Be("Modificado");
        tx.Date.Should().Be(newDate);
        tx.CategoryId.Should().Be(categoryId);
        tx.Tags.Should().Be("nuevo");
        _accountRepoMock.Verify(r => r.Update(account), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTransactionNotFound_ShouldReturnFailure()
    {
        _accountRepoMock.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var command = new UpdateTransactionCommand(Guid.NewGuid(), "Desc", DateTime.UtcNow);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no encontrada");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
