using System;

namespace FinTrack.Application.Debts.DTOs;

public class CreateDebtRequest
{
    public string Creditor { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Type { get; set; } = "Owed";
    public DateTime? DueDate { get; set; }
    public string? Description { get; set; }
    public decimal InterestRate { get; set; } = 0;
}
