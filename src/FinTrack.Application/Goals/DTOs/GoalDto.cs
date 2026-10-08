using System;

namespace FinTrack.Application.Goals.DTOs;

public class GoalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
    public decimal RemainingAmount { get; set; }
}
