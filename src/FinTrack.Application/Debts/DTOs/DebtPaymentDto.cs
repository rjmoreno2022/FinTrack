using System;

namespace FinTrack.Application.Debts.DTOs;

public class DebtPaymentDto
{
    public Guid Id { get; set; }
    public Guid DebtId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
}
