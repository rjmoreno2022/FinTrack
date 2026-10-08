using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FinTrack.Application.Accounts.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Accounts;

public class CreateAccountHandlerTests
{
    private readonly Mock<IRepository<Account>> _accountRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateAccountHandler _handler;

    public CreateAccountHandlerTests()
    {
        _accountRepoMock = new Mock<IRepository<Account>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateAccountHandler(_accountRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateAccountAndReturnId()
    {
        // Arrange
        var command = new CreateAccountCommand("Cuenta Principal", "Bank", "USD", "Mi cuenta corriente");

        _accountRepoMock.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _accountRepoMock.Verify(r => r.AddAsync(It.Is<Account>(a =>
            a.Name == "Cuenta Principal" &&
            a.Type == AccountType.Bank &&
            a.Currency == Currency.USD &&
            a.Description == "Mi cuenta corriente"), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidAccountType_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateAccountCommand("Cuenta", "Inexistente", "USD", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Tipo de cuenta inválido");
        _accountRepoMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidCurrency_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateAccountCommand("Cuenta", "Cash", "XYZ", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Moneda inválida");
        _accountRepoMock.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
