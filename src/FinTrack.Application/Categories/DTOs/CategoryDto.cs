using System;
using System.Collections.Generic;

namespace FinTrack.Application.Categories.DTOs;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public bool IsSystem { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new();
}
