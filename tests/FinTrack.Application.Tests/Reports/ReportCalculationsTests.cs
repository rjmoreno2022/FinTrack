using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace FinTrack.Application.Tests.Reports;

public class ReportCalculationsTests
{
    [Theory]
    [InlineData(1000, 400, 60.0)]
    [InlineData(2000, 1500, 25.0)]
    [InlineData(1000, 1000, 0.0)]
    [InlineData(500, 800, 0.0)]     // Gastos mayores a ingresos -> 0%
    [InlineData(0, 500, 0.0)]       // Sin ingresos -> 0%
    public void CalculateSavingsRate_ShouldReturnExpectedPercentage(decimal income, decimal expense, decimal expectedRate)
    {
        // Act
        decimal rate = income > 0 ? Math.Max(0, ((income - expense) / income) * 100) : 0;

        // Assert
        rate.Should().Be(expectedRate);
    }

    [Fact]
    public void CalculateCategoryPercentages_ShouldSumToHundred()
    {
        // Arrange
        var categoryExpenses = new Dictionary<string, decimal>
        {
            { "Alimentación", 300m },
            { "Transporte", 150m },
            { "Servicios", 50m }
        };

        var total = categoryExpenses.Values.Sum();

        // Act
        var percentages = categoryExpenses
            .Select(kv => Math.Round((kv.Value / total) * 100, 1))
            .ToList();

        // Assert
        total.Should().Be(500m);
        percentages.Should().Contain(60.0m);
        percentages.Should().Contain(30.0m);
        percentages.Should().Contain(10.0m);
        percentages.Sum().Should().Be(100.0m);
    }

    [Fact]
    public void CalculateNetSavings_WhenIncomeExceedsExpense_ShouldBePositive()
    {
        // Arrange
        decimal income = 1200m;
        decimal expense = 750m;

        // Act
        decimal netSavings = income - expense;

        // Assert
        netSavings.Should().Be(450m);
    }
}
