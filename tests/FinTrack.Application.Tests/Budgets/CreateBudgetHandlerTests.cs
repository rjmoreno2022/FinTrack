using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FinTrack.Application.Budgets.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Budgets;

public class CreateBudgetHandlerTests
{
    private readonly Mock<IRepository<Budget>> _budgetRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateBudgetHandler _handler;

    public CreateBudgetHandlerTests()
    {
        _budgetRepoMock = new Mock<IRepository<Budget>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateBudgetHandler(_budgetRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateBudgetAndReturnId()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            "Alimentación Mensual",
            500m,
            "USD",
            "Monthly",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddMonths(1)
        );

        _budgetRepoMock.Setup(r => r.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _budgetRepoMock.Verify(r => r.AddAsync(It.Is<Budget>(b =>
            b.Name == "Alimentación Mensual" &&
            b.LimitAmount == 500m &&
            b.Currency == Currency.USD &&
            b.Period == BudgetPeriod.Monthly), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidDates_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            "Invalido",
            200m,
            "USD",
            "Monthly",
            DateTime.UtcNow.Date.AddDays(5),
            DateTime.UtcNow.Date // Fin antes de inicio
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("fecha de inicio debe ser anterior");
        _budgetRepoMock.Verify(r => r.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidPeriod_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            "Invalido",
            200m,
            "USD",
            "PeriodoDesconocido",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddMonths(1)
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Período de presupuesto inválido");
    }
}
