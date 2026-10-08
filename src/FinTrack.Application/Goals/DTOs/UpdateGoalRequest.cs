using System;

namespace FinTrack.Application.Goals.DTOs;

public class UpdateGoalRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
}
