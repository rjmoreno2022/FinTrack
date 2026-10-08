using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FinTrack.Application.Debts.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Debts;

public class RegisterDebtPaymentHandlerTests
{
    private readonly Mock<IRepository<Debt>> _debtRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RegisterDebtPaymentHandler _handler;

    public RegisterDebtPaymentHandlerTests()
    {
        _debtRepoMock = new Mock<IRepository<Debt>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new RegisterDebtPaymentHandler(_debtRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidPartialPayment_ShouldReduceRemainingAmount()
    {
        // Arrange
        var debt = new Debt("Banco Nacional", 1000m, Currency.USD, DebtType.Owed);
        _debtRepoMock.Setup(r => r.GetByIdAsync(debt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(debt);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RegisterDebtPaymentCommand(debt.Id, 400m, DateTime.UtcNow, "Abono inicial");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        debt.RemainingAmount.Should().Be(600m);
        debt.Status.Should().Be(DebtStatus.Active);
        debt.Payments.Should().HaveCount(1);

        _debtRepoMock.Verify(r => r.Update(debt), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFullPayment_ShouldMarkDebtAsPaid()
    {
        // Arrange
        var debt = new Debt("Amigo", 250m, Currency.USD, DebtType.Owed);
        _debtRepoMock.Setup(r => r.GetByIdAsync(debt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(debt);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RegisterDebtPaymentCommand(debt.Id, 250m, DateTime.UtcNow, "Pago total");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        debt.RemainingAmount.Should().Be(0m);
        debt.Status.Should().Be(DebtStatus.Paid);

        _debtRepoMock.Verify(r => r.Update(debt), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAmountExceedsRemaining_ShouldReturnFailure()
    {
        // Arrange
        var debt = new Debt("Banco", 100m, Currency.USD, DebtType.Owed);
        _debtRepoMock.Setup(r => r.GetByIdAsync(debt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(debt);

        var command = new RegisterDebtPaymentCommand(debt.Id, 150m, DateTime.UtcNow, "Exceso");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no puede exceder el monto pendiente");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
