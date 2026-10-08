using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FinTrack.Application.Goals.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Events;
using FinTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Goals;

public class ContributeGoalHandlerTests
{
    private readonly Mock<IRepository<Goal>> _goalRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ContributeGoalHandler _handler;

    public ContributeGoalHandlerTests()
    {
        _goalRepoMock = new Mock<IRepository<Goal>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new ContributeGoalHandler(_goalRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidAmount_ShouldContributeAndPersist()
    {
        // Arrange
        var goal = new Goal("Vacaciones", 1000m, Currency.USD);
        _goalRepoMock.Setup(r => r.GetByIdAsync(goal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ContributeGoalCommand(goal.Id, 300m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        goal.CurrentAmount.Should().Be(300m);
        goal.ProgressPercentage.Should().Be(30m);
        goal.Status.Should().Be(GoalStatus.Active);

        _goalRepoMock.Verify(r => r.Update(goal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTargetReached_ShouldCompleteGoalAndRaiseEvent()
    {
        // Arrange
        var goal = new Goal("Fondo de Emergencia", 500m, Currency.USD);
        _goalRepoMock.Setup(r => r.GetByIdAsync(goal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ContributeGoalCommand(goal.Id, 500m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        goal.CurrentAmount.Should().Be(500m);
        goal.Status.Should().Be(GoalStatus.Completed);
        goal.DomainEvents.Should().ContainSingle(e => e is GoalReachedEvent);

        var domainEvent = goal.DomainEvents.First() as GoalReachedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.GoalId.Should().Be(goal.Id);
        domainEvent.GoalName.Should().Be("Fondo de Emergencia");
    }

    [Fact]
    public async Task Handle_WhenGoalNotFound_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _goalRepoMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var command = new ContributeGoalCommand(nonExistentId, 100m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Meta no encontrada");
    }
}
