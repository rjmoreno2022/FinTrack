using System;

namespace FinTrack.Application.Debts.DTOs;

public class DebtDto
{
    public Guid Id { get; set; }
    public string Creditor { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime DateIncurred { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal InterestRate { get; set; }
    public decimal ProgressPercentage { get; set; }
}
