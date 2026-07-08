using System;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FinTrack.Domain.Events;

namespace FinTrack.Domain.Entities;

/// <summary>
/// Representa una meta de ahorro (ej. Fondo de Emergencia) y actúa como raíz del agregado.
/// </summary>
public class Goal : Entity, IAggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public Currency Currency { get; private set; }
    public DateTime? TargetDate { get; private set; }
    public GoalStatus Status { get; private set; }
    public GoalPriority Priority { get; private set; }

    // EF Core requiere constructor vacío
    private Goal() 
    {
        Name = null!;
    }

    public Goal(string name, decimal targetAmount, Currency currency,
        DateTime? targetDate = null, string? description = null,
        GoalPriority priority = GoalPriority.Medium)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la meta es requerido");
        if (targetAmount <= 0)
            throw new DomainException("El monto objetivo debe ser mayor a cero");

        Name = name;
        TargetAmount = targetAmount;
        CurrentAmount = 0;
        Currency = currency;
        TargetDate = targetDate;
        Description = description;
        Priority = priority;
        Status = GoalStatus.Active;
    }

    public void Contribute(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("La contribución debe ser positiva");
        if (Status != GoalStatus.Active)
            throw new DomainException("Solo metas activas pueden recibir contribuciones");

        CurrentAmount += amount;

        if (CurrentAmount >= TargetAmount)
        {
            CurrentAmount = TargetAmount;
            Status = GoalStatus.Completed;
            AddDomainEvent(new GoalReachedEvent(Id, Name));
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0 || amount > CurrentAmount)
            throw new DomainException("Monto de retiro inválido");

        CurrentAmount -= amount;

        if (Status == GoalStatus.Completed && CurrentAmount < TargetAmount)
            Status = GoalStatus.Active;

        UpdatedAt = DateTime.UtcNow;
    }

    public decimal ProgressPercentage =>
        TargetAmount > 0 ? (CurrentAmount / TargetAmount) * 100 : 0;

    public decimal RemainingAmount => TargetAmount - CurrentAmount;
}
