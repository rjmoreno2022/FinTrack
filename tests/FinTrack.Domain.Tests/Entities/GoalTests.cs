using System;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FinTrack.Domain.Tests.Entities;

public class GoalTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateGoal()
    {
        var goal = new Goal("Ahorro Emergencia", 1000m, Currency.USD);

        goal.Name.Should().Be("Ahorro Emergencia");
        goal.TargetAmount.Should().Be(1000m);
        goal.CurrentAmount.Should().Be(0);
        goal.Status.Should().Be(GoalStatus.Active);
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateGoal()
    {
        var goal = new Goal("Vacaciones", 800m, Currency.USD);
        goal.Contribute(200m);

        var newTargetDate = DateTime.UtcNow.AddYears(1);
        goal.Update("Vacaciones 2027", 1200m, newTargetDate, "Viaje a la playa", GoalPriority.High);

        goal.Name.Should().Be("Vacaciones 2027");
        goal.TargetAmount.Should().Be(1200m);
        goal.Description.Should().Be("Viaje a la playa");
        goal.Priority.Should().Be(GoalPriority.High);
        goal.TargetDate.Should().Be(newTargetDate);
    }

    [Fact]
    public void Update_WithTargetAmountLowerThanCurrent_ShouldThrowDomainException()
    {
        var goal = new Goal("Laptop", 1000m, Currency.USD);
        goal.Contribute(500m);

        var act = () => goal.Update("Laptop", 400m);

        act.Should().Throw<DomainException>()
            .WithMessage("El monto objetivo no puede ser menor al monto ahorrado actual");
    }
}
