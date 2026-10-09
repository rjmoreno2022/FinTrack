using System;
using System.Collections.Generic;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FinTrack.Domain.Events;

namespace FinTrack.Domain.Entities;

/// <summary>
/// Cuenta financiera del usuario (Banco, Efectivo, Binance, etc.) que actúa como raíz del agregado.
/// </summary>
public class Account : Entity, IAggregateRoot, IUserOwned
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public AccountType Type { get; private set; }
    public Currency Currency { get; private set; }
    public decimal Balance { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<Transaction> Transactions { get; private set; }

    // EF Core requiere constructor vacío
    private Account()
    {
        Name = null!;
        Transactions = new List<Transaction>();
    }

    public Account(string name, AccountType type, Currency currency, string? description = null, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la cuenta es requerido");

        Name = name;
        Type = type;
        Currency = currency;
        Balance = 0;
        Description = description;
        IsActive = true;
        Transactions = new List<Transaction>();

        if (userId.HasValue)
            AssignOwner(userId.Value);
    }

    public void AssignOwner(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("El ID de usuario no puede ser vacío");

        if (UserId != Guid.Empty && UserId != userId)
            throw new DomainException("La cuenta ya tiene un dueño asignado");

        UserId = userId;
    }

    public Transaction AddTransaction(TransactionType type, decimal amount,
        string description, DateTime date, Guid? categoryId = null,
        string? tags = null, bool isRecurring = false)
    {
        if (!IsActive)
            throw new DomainException("No se pueden agregar transacciones a una cuenta inactiva");

        var transaction = new Transaction(Id, type, amount, Currency,
            description, date, categoryId, tags, isRecurring);

        UpdateBalance(type, amount);
        Transactions.Add(transaction);

        AddDomainEvent(new TransactionCreatedEvent(transaction.Id, Id, amount, type));

        return transaction;
    }

    public void RemoveTransaction(Guid transactionId)
    {
        var transaction = Transactions.FirstOrDefault(t => t.Id == transactionId);
        if (transaction == null)
            throw new DomainException("Transacción no encontrada en esta cuenta");

        Balance = transaction.Type switch
        {
            TransactionType.Income => Balance - transaction.Amount,
            TransactionType.Expense => Balance + transaction.Amount,
            TransactionType.Transfer => Balance + transaction.Amount,
            _ => throw new DomainException("Tipo de transacción no válido")
        };

        Transactions.Remove(transaction);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTransaction(Guid transactionId, string description, DateTime date, Guid? categoryId = null, string? tags = null)
    {
        var transaction = Transactions.FirstOrDefault(t => t.Id == transactionId);
        if (transaction == null)
            throw new DomainException("Transacción no encontrada en esta cuenta");

        transaction.Update(description, date, categoryId, tags);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la cuenta es requerido");

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private void UpdateBalance(TransactionType type, decimal amount)
    {
        Balance = type switch
        {
            TransactionType.Income => Balance + amount,
            TransactionType.Expense => Balance - amount,
            TransactionType.Transfer => Balance - amount, // La cuenta origen descuenta el monto de la transferencia
            _ => throw new DomainException("Tipo de transacción no válido")
        };
    }
}
