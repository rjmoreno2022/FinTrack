using System;
using FinTrack.Domain.Common;
using FinTrack.Domain.Exceptions;

namespace FinTrack.Domain.Entities;

/// <summary>
/// Representa un pago parcial realizado a una deuda.
/// </summary>
public class DebtPayment : Entity
{
    public Guid DebtId { get; private set; }
    public Debt Debt { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public string? Note { get; private set; }

    // EF Core requiere constructor vacío
    private DebtPayment() { }

    public DebtPayment(Guid debtId, decimal amount, DateTime date, string? note = null)
    {
        if (amount <= 0)
            throw new DomainException("El monto del pago debe ser mayor a cero");

        DebtId = debtId;
        Amount = amount;
        Date = date;
        Note = note;
    }
}
