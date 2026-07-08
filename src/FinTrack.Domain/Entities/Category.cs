using System;
using System.Collections.Generic;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;

namespace FinTrack.Domain.Entities;

/// <summary>
/// Representa una categoría jerárquica para clasificar ingresos o gastos.
/// </summary>
public class Category : Entity
{
    public string Name { get; private set; }
    public CategoryType Type { get; private set; }
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }
    public bool IsSystem { get; private set; }
    public ICollection<Category> SubCategories { get; private set; }

    // EF Core requiere un constructor privado vacío
    private Category()
    {
        Name = null!;
        SubCategories = new List<Category>();
    }

    public Category(string name, CategoryType type, string? icon = null, string? color = null, Guid? parentCategoryId = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la categoría es requerido");

        Name = name;
        Type = type;
        Icon = icon;
        Color = color;
        ParentCategoryId = parentCategoryId;
        IsSystem = isSystem;
        SubCategories = new List<Category>();
    }

    public void Update(string name, string? icon, string? color)
    {
        if (IsSystem)
            throw new DomainException("No se pueden modificar categorías del sistema");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la categoría es requerido");

        Name = name;
        Icon = icon;
        Color = color;
        UpdatedAt = DateTime.UtcNow;
    }
}
