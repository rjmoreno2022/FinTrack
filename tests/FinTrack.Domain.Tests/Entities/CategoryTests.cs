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

    [Fact]
    public void AssignOwner_WithValidGuid_ShouldSetUserId()
    {
        var category = new Category("Personalizada", CategoryType.Expense);
        var userId = Guid.NewGuid();

        category.AssignOwner(userId);

        category.UserId.Should().Be(userId);
    }

    [Fact]
    public void AssignOwner_WhenIsSystemCategory_ShouldThrowDomainException()
    {
        var category = new Category("General", CategoryType.Expense, isSystem: true);
        var userId = Guid.NewGuid();

        var act = () => category.AssignOwner(userId);

        act.Should().Throw<DomainException>()
            .WithMessage("No se puede asignar dueño a una categoría del sistema");
    }

    [Fact]
    public void AssignOwner_WithEmptyGuid_ShouldThrowDomainException()
    {
        var category = new Category("Personalizada", CategoryType.Expense);

        var act = () => category.AssignOwner(Guid.Empty);

        act.Should().Throw<DomainException>()
            .WithMessage("El ID de usuario no puede ser vacío");
    }

    [Fact]
    public void AssignOwner_WhenAlreadyAssignedToDifferentUser_ShouldThrowDomainException()
    {
        var owner1 = Guid.NewGuid();
        var owner2 = Guid.NewGuid();
        var category = new Category("Personalizada", CategoryType.Expense, userId: owner1);

        var act = () => category.AssignOwner(owner2);

        act.Should().Throw<DomainException>()
            .WithMessage("La categoría ya tiene un dueño asignado");
    }

    [Fact]
    public void AssignOwner_WhenReassignedToSameUser_ShouldSucceed()
    {
        var owner = Guid.NewGuid();
        var category = new Category("Personalizada", CategoryType.Expense, userId: owner);

        var act = () => category.AssignOwner(owner);

        act.Should().NotThrow();
        category.UserId.Should().Be(owner);
    }
}
