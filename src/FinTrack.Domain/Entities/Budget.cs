using System;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;

namespace FinTrack.Domain.Entities;

/// <summary>
/// Representa un presupuesto definido para gastos por categoría o generales durante un período.
/// </summary>
public class Budget : Entity
{
    public string Name { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public decimal LimitAmount { get; private set; }
    public decimal SpentAmount { get; private set; }
    public BudgetPeriod Period { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public Currency Currency { get; private set; }
    public bool IsActive { get; private set; }

    public decimal Remaining => LimitAmount - SpentAmount;
    public decimal UsagePercentage =>
        LimitAmount > 0 ? (SpentAmount / LimitAmount) * 100 : 0;
    public bool IsOverBudget => SpentAmount > LimitAmount;

    // EF Core requiere constructor vacío
    private Budget() 
    {
        Name = null!;
    }

    public Budget(string name, decimal limitAmount, Currency currency,
        BudgetPeriod period, DateTime startDate, DateTime endDate,
        Guid? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del presupuesto es requerido");
        if (limitAmount <= 0)
            throw new DomainException("El límite debe ser mayor a cero");
        if (startDate >= endDate)
            throw new DomainException("La fecha de inicio debe ser anterior a la fecha de fin");

        Name = name;
        LimitAmount = limitAmount;
        Currency = currency;
        Period = period;
        StartDate = startDate;
        EndDate = endDate;
        CategoryId = categoryId;
        SpentAmount = 0;
        IsActive = true;
    }

    public void RecordSpending(decimal amount)
    {
        if (amount < 0) return;
        SpentAmount += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReduceSpending(decimal amount)
    {
        SpentAmount = Math.Max(0, SpentAmount - amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rollover(DateTime newStartDate, DateTime newEndDate)
    {
        if (newStartDate >= newEndDate)
            throw new DomainException("La fecha de inicio debe ser anterior a la fecha de fin");

        SpentAmount = 0;
        StartDate = newStartDate;
        EndDate = newEndDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
