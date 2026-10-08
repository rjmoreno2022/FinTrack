using System;
using System.Threading;
using System.Threading.Tasks;
using FinTrack.Application.Goals.Commands;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace FinTrack.Application.Tests.Goals;

public class UpdateGoalHandlerTests
{
    private readonly Mock<IRepository<Goal>> _goalRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateGoalHandler _handler;

    public UpdateGoalHandlerTests()
    {
        _goalRepoMock = new Mock<IRepository<Goal>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateGoalHandler(_goalRepoMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateGoalAndReturnSuccess()
    {
        var goal = new Goal("Ahorro", 500m, Currency.USD);

        _goalRepoMock.Setup(r => r.GetByIdAsync(goal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateGoalCommand(goal.Id, "Nuevo Ahorro", 800m, DateTime.UtcNow.AddMonths(4), "Nueva desc", "High");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        goal.Name.Should().Be("Nuevo Ahorro");
        goal.TargetAmount.Should().Be(800m);
        goal.Priority.Should().Be(GoalPriority.High);
        _goalRepoMock.Verify(r => r.Update(goal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ShouldReturnFailure()
    {
        _goalRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        var command = new UpdateGoalCommand(Guid.NewGuid(), "Nombre", 500m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no encontrada");
    }
}
