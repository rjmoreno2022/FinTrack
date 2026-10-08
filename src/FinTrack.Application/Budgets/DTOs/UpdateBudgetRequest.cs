using System;

namespace FinTrack.Application.Budgets.DTOs;

public class UpdateBudgetRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal LimitAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? CategoryId { get; set; }
}
