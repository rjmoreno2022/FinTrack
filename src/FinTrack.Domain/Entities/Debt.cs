using System;
using System.Collections.Generic;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;

namespace FinTrack.Domain.Entities;

/// <summary>
/// Representa una deuda activa (dinero adeudado o por cobrar) que actúa como raíz del agregado.
/// </summary>
public class Debt : Entity, IAggregateRoot
{
    public string Creditor { get; private set; }
    public string? Description { get; private set; }
    public decimal OriginalAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public Currency Currency { get; private set; }
    public DebtType Type { get; private set; }
    public DateTime DateIncurred { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DebtStatus Status { get; private set; }
    public decimal InterestRate { get; private set; }
    public ICollection<DebtPayment> Payments { get; private set; }

    // EF Core requiere constructor vacío
    private Debt()
    {
        Creditor = null!;
        Payments = new List<DebtPayment>();
    }

    public Debt(string creditor, decimal originalAmount, Currency currency,
        DebtType type, DateTime? dueDate = null, string? description = null,
        decimal interestRate = 0)
    {
        if (string.IsNullOrWhiteSpace(creditor))
            throw new DomainException("El acreedor es requerido");
        if (originalAmount <= 0)
            throw new DomainException("El monto debe ser mayor a cero");

        Creditor = creditor;
        OriginalAmount = originalAmount;
        RemainingAmount = originalAmount;
        Currency = currency;
        Type = type;
        DateIncurred = DateTime.UtcNow;
        DueDate = dueDate;
        Description = description;
        InterestRate = interestRate;
        Status = DebtStatus.Active;
        Payments = new List<DebtPayment>();
    }

    public void RegisterPayment(decimal amount, DateTime date, string? note = null)
    {
        if (amount <= 0)
            throw new DomainException("El pago debe ser mayor a cero");
        if (amount > RemainingAmount)
            throw new DomainException("El pago no puede exceder el monto pendiente");
        if (Status != DebtStatus.Active)
            throw new DomainException("No se pueden registrar pagos en una deuda no activa");

        var payment = new DebtPayment(Id, amount, date, note);
        Payments.Add(payment);
        RemainingAmount -= amount;

        if (RemainingAmount == 0)
            Status = DebtStatus.Paid;

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDefaulted()
    {
        if (Status == DebtStatus.Paid)
            throw new DomainException("Una deuda pagada no puede marcarse como incumplida");
        Status = DebtStatus.Defaulted;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal ProgressPercentage =>
        OriginalAmount > 0 ? ((OriginalAmount - RemainingAmount) / OriginalAmount) * 100 : 0;
}
