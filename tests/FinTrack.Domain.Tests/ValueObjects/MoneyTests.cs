using System;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FinTrack.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinTrack.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateMoney()
    {
        // Arrange & Act
        var money = new Money(100.50m, Currency.USD);

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowDomainException()
    {
        // Act
        Action act = () => new Money(-50m, Currency.USD);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("El monto no puede ser negativo");
    }

    [Fact]
    public void Add_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var m1 = new Money(100m, Currency.USD);
        var m2 = new Money(50m, Currency.USD);

        // Act
        var result = m1.Add(m2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ShouldThrowDomainException()
    {
        // Arrange
        var usd = new Money(100m, Currency.USD);
        var ves = new Money(50m, Currency.VES);

        // Act
        Action act = () => usd.Add(ves);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("No se pueden sumar montos de diferentes monedas");
    }

    [Fact]
    public void Subtract_WithSameCurrency_ShouldReturnDifference()
    {
        // Arrange
        var m1 = new Money(100m, Currency.USD);
        var m2 = new Money(30m, Currency.USD);

        // Act
        var result = m1.Subtract(m2);

        // Assert
        result.Amount.Should().Be(70m);
        result.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Subtract_WithDifferentCurrencies_ShouldThrowDomainException()
    {
        // Arrange
        var usd = new Money(100m, Currency.USD);
        var ves = new Money(30m, Currency.VES);

        // Act
        Action act = () => usd.Subtract(ves);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("No se pueden restar montos de diferentes monedas");
    }

    [Fact]
    public void ConvertTo_WithDifferentCurrencyAndValidRate_ShouldReturnConvertedMoney()
    {
        // Arrange
        var usd = new Money(10m, Currency.USD);
        var rate = 36.50m; // 1 USD = 36.50 VES

        // Act
        var result = usd.ConvertTo(Currency.VES, rate);

        // Assert
        result.Amount.Should().Be(365.0m);
        result.Currency.Should().Be(Currency.VES);
    }

    [Fact]
    public void ConvertTo_WithSameCurrency_ShouldReturnOriginalMoney()
    {
        // Arrange
        var usd = new Money(10m, Currency.USD);
        var rate = 1.0m;

        // Act
        var result = usd.ConvertTo(Currency.USD, rate);

        // Assert
        result.Should().Be(usd);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void ConvertTo_WithInvalidRate_ShouldThrowDomainException(decimal invalidRate)
    {
        // Arrange
        var usd = new Money(10m, Currency.USD);

        // Act
        Action act = () => usd.ConvertTo(Currency.VES, invalidRate);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("La tasa de cambio para la conversión debe ser mayor a cero");
    }

    [Fact]
    public void Equality_WithSameAmountAndCurrency_ShouldBeEqual()
    {
        // Arrange
        var m1 = new Money(100m, Currency.USD);
        var m2 = new Money(100m, Currency.USD);

        // Assert
        m1.Should().Be(m2);
        (m1 == m2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentAmountOrCurrency_ShouldNotBeEqual()
    {
        // Arrange
        var usd100 = new Money(100m, Currency.USD);
        var usd50 = new Money(50m, Currency.USD);
        var ves100 = new Money(100m, Currency.VES);

        // Assert
        usd100.Should().NotBe(usd50);
        usd100.Should().NotBe(ves100);
        (usd100 != usd50).Should().BeTrue();
    }
}
