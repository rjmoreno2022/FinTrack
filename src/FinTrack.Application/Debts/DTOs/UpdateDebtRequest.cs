using System;

namespace FinTrack.Application.Debts.DTOs;

public class UpdateDebtRequest
{
    public string Creditor { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? Description { get; set; }
    public decimal InterestRate { get; set; } = 0;
}
