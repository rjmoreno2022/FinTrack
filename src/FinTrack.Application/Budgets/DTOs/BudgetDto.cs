using System;

namespace FinTrack.Application.Budgets.DTOs;

public class BudgetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal LimitAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal Remaining { get; set; }
    public decimal UsagePercentage { get; set; }
    public bool IsOverBudget { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
