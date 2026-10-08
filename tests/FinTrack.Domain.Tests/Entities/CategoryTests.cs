using System;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FinTrack.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateCategory()
    {
        var category = new Category("Comida", CategoryType.Expense, "fastfood", "#FF0000");

        category.Name.Should().Be("Comida");
        category.Type.Should().Be(CategoryType.Expense);
        category.Icon.Should().Be("fastfood");
        category.Color.Should().Be("#FF0000");
        category.IsSystem.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        var act = () => new Category(invalidName!, CategoryType.Expense);

        act.Should().Throw<DomainException>()
            .WithMessage("El nombre de la categoría es requerido");
    }

    [Fact]
    public void Update_ShouldModifyNameIconAndColor()
    {
        var category = new Category("Transporte", CategoryType.Expense, "directions_car", "#00FF00");

        category.Update("Transporte Público", "directions_bus", "#0000FF");

        category.Name.Should().Be("Transporte Público");
        category.Icon.Should().Be("directions_bus");
        category.Color.Should().Be("#0000FF");
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WhenIsSystemCategory_ShouldThrowDomainException()
    {
        var category = new Category("General", CategoryType.Expense, null, null, null, isSystem: true);

        var act = () => category.Update("Nuevo Nombre", null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("No se pueden modificar categorías del sistema");
    }
}
