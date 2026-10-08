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

public class UpdateBudgetHandlerTests
{
    private readonly Mock<IRepository<Budget>> _budgetRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateBudgetHandler _handler;

    public UpdateBudgetHandlerTests()
    {
        _budgetRepoMock = new Mock<IRepository<Budget>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateBudgetHandler(_budgetRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateBudgetAndReturnSuccess()
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddMonths(1);
        var budget = new Budget("Original", 200m, Currency.USD, BudgetPeriod.Monthly, start, end);

        _budgetRepoMock.Setup(r => r.GetByIdAsync(budget.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var newEnd = start.AddMonths(2);
        var command = new UpdateBudgetCommand(budget.Id, "Modificado", 350m, start, newEnd);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        budget.Name.Should().Be("Modificado");
        budget.LimitAmount.Should().Be(350m);
        budget.EndDate.Should().Be(newEnd);
        _budgetRepoMock.Verify(r => r.Update(budget), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ShouldReturnFailure()
    {
        _budgetRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget?)null);

        var command = new UpdateBudgetCommand(Guid.NewGuid(), "Nombre", 100m, DateTime.UtcNow, DateTime.UtcNow.AddDays(10));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no encontrado");
    }
}
