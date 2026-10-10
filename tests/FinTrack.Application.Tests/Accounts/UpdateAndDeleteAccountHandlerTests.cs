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

public class UpdateAndDeleteAccountHandlerTests
{
    private readonly Mock<IRepository<Account>> _accountRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task UpdateAccount_WhenAccountExists_ShouldUpdateAndReturnSuccess()
    {
        // Arrange
        var account = new Account("Old Name", AccountType.Bank, Currency.USD, "Old Desc");
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new UpdateAccountHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
        var command = new UpdateAccountCommand(account.Id, "New Name", "New Desc");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.Name.Should().Be("New Name");
        account.Description.Should().Be("New Desc");
        _accountRepoMock.Verify(r => r.Update(account), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAccount_WhenAccountNotFound_ShouldReturnFail()
    {
        // Arrange
        _accountRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var handler = new UpdateAccountHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
        var command = new UpdateAccountCommand(Guid.NewGuid(), "New Name", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no encontrada");
    }

    [Fact]
    public async Task DeleteAccount_WhenAccountExists_ShouldDeactivateAndReturnSuccess()
    {
        // Arrange
        var account = new Account("Account to Delete", AccountType.Cash, Currency.USD);
        account.IsActive.Should().BeTrue();

        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new DeleteAccountHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
        var command = new DeleteAccountCommand(account.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.IsActive.Should().BeFalse();
        _accountRepoMock.Verify(r => r.Update(account), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_WhenNotFound_ShouldReturnFail()
    {
        // Arrange
        _accountRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var handler = new DeleteAccountHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
        var command = new DeleteAccountCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no encontrada");
    }
}
