using System;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;

namespace FinTrack.Domain.Entities;

/// <summary>
/// Representa una transacción individual (ingreso, gasto o transferencia) asociada a una cuenta.
/// </summary>
public class Transaction : Entity
{
    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = null!;
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public string Description { get; private set; }
    public DateTime Date { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public string? Tags { get; private set; }
    public bool IsRecurring { get; private set; }
    public Guid? TransferAccountId { get; private set; }
    public Guid? RelatedTransactionId { get; private set; }

    // EF Core requiere constructor vacío
    private Transaction() 
    {
        Description = null!;
    }

    public Transaction(Guid accountId, TransactionType type, decimal amount,
        Currency currency, string description, DateTime date,
        Guid? categoryId = null, string? tags = null,
        bool isRecurring = false)
    {
        if (amount <= 0)
            throw new DomainException("El monto debe ser mayor a cero");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("La descripción es requerida");

        AccountId = accountId;
        Type = type;
        Amount = amount;
        Currency = currency;
        Description = description;
        Date = date;
        CategoryId = categoryId;
        Tags = tags;
        IsRecurring = isRecurring;
    }

    public void Update(string description, Guid? categoryId, string? tags)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("La descripción es requerida");

        Description = description;
        CategoryId = categoryId;
        Tags = tags;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTransferInfo(Guid transferAccountId, Guid relatedTransactionId)
    {
        TransferAccountId = transferAccountId;
        RelatedTransactionId = relatedTransactionId;
        UpdatedAt = DateTime.UtcNow;
    }
}
