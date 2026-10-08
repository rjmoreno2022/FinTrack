using System;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FinTrack.Domain.Tests.Entities;

public class BudgetTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateBudget()
    {
        var start = DateTime.UtcNow;
        var end = start.AddMonths(1);

        var budget = new Budget("Presupuesto Mensual", 500m, Currency.USD, BudgetPeriod.Monthly, start, end);

        budget.Name.Should().Be("Presupuesto Mensual");
        budget.LimitAmount.Should().Be(500m);
        budget.SpentAmount.Should().Be(0);
        budget.Currency.Should().Be(Currency.USD);
        budget.Period.Should().Be(BudgetPeriod.Monthly);
        budget.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateBudget()
    {
        var start = DateTime.UtcNow;
        var end = start.AddMonths(1);
        var budget = new Budget("Original", 300m, Currency.USD, BudgetPeriod.Monthly, start, end);

        var newEnd = start.AddMonths(2);
        var newCatId = Guid.NewGuid();

        budget.Update("Modificado", 450m, newCatId, start, newEnd);

        budget.Name.Should().Be("Modificado");
        budget.LimitAmount.Should().Be(450m);
        budget.CategoryId.Should().Be(newCatId);
        budget.EndDate.Should().Be(newEnd);
        budget.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithInvalidLimit_ShouldThrowDomainException()
    {
        var start = DateTime.UtcNow;
        var end = start.AddMonths(1);
        var budget = new Budget("Original", 300m, Currency.USD, BudgetPeriod.Monthly, start, end);

        var act = () => budget.Update("Modificado", 0m, null, start, end);

        act.Should().Throw<DomainException>()
            .WithMessage("El límite debe ser mayor a cero");
    }
}
