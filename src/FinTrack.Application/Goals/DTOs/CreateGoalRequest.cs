using System;

namespace FinTrack.Application.Goals.DTOs;

public class CreateGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? TargetDate { get; set; }
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
}
