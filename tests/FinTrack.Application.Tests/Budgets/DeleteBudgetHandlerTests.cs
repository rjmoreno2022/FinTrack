using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Budgets.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Budgets;

public class DeleteBudgetHandlerTests
{
    private readonly Mock<IRepository<Budget>> _budgetRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task DeleteBudget_WhenExists_ShouldDeleteAndReturnSuccess()
    {
        // Arrange
        var budget = new Budget("Presupuesto Mercado", 500m, Currency.USD, BudgetPeriod.Monthly, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));
        _budgetRepoMock.Setup(r => r.GetByIdAsync(budget.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

        var handler = new DeleteBudgetHandler(_budgetRepoMock.Object, _unitOfWorkMock.Object);
        var command = new DeleteBudgetCommand(budget.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _budgetRepoMock.Verify(r => r.Delete(budget), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBudget_WhenNotFound_ShouldFail()
    {
        // Arrange
        _budgetRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget?)null);

        var handler = new DeleteBudgetHandler(_budgetRepoMock.Object, _unitOfWorkMock.Object);
        var command = new DeleteBudgetCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no encontrado");
    }
}
