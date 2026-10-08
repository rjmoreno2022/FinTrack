using System;

namespace FinTrack.Application.Budgets.DTOs;

public class CreateBudgetRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal LimitAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Period { get; set; } = "Monthly";
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddMonths(1);
    public Guid? CategoryId { get; set; }
}
