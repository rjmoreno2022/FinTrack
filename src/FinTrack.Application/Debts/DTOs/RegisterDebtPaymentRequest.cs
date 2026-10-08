using System;

namespace FinTrack.Application.Debts.DTOs;

public class RegisterDebtPaymentRequest
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
