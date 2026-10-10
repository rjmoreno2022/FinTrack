using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Goals.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Goals;

public class CreateGoalHandlerTests
{
    private readonly Mock<IRepository<Goal>> _goalRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task CreateGoal_WithValidData_ShouldPersistAndReturnId()
    {
        // Arrange
        var handler = new CreateGoalHandler(_goalRepoMock.Object, _unitOfWorkMock.Object);
        var command = new CreateGoalCommand("Fondo de Emergencia", 2000m, "USD", DateTime.UtcNow.AddYears(1), "Para imprevistos", "High");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _goalRepoMock.Verify(r => r.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateGoal_WithInvalidCurrency_ShouldFail()
    {
        // Arrange
        var handler = new CreateGoalHandler(_goalRepoMock.Object, _unitOfWorkMock.Object);
        var command = new CreateGoalCommand("Meta", 1000m, "INVALID_CURRENCY");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Moneda inválida");
    }

    [Fact]
    public async Task CreateGoal_WithInvalidPriority_ShouldFail()
    {
        // Arrange
        var handler = new CreateGoalHandler(_goalRepoMock.Object, _unitOfWorkMock.Object);
        var command = new CreateGoalCommand("Meta", 1000m, "USD", Priority: "UltraMegaHigh");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Prioridad inválida");
    }
}
